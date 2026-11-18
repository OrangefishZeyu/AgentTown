import io
import json
from pathlib import Path
import tempfile
import threading
import unittest
from unittest.mock import patch
import urllib.request
import urllib.error
from http.server import ThreadingHTTPServer
from server import TownService, ProviderError, make_handler, validate_chat

class TownTests(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory()
        self.service=TownService(Path(self.temp.name),api_key='')
    def tearDown(self):
        for h in self.service.log.handlers[:]: h.close();self.service.log.removeHandler(h)
        self.temp.cleanup()
    def chat(self,message,npc='张三',player='test'):
        return self.service.chat({'npc_name':npc,'message':message,'player_id':player})
    def test_long_term_recall_after_restart(self):
        self.chat('我叫小林，我喜欢咖啡。')
        other=TownService(Path(self.temp.name),api_key='')
        try:
            result=other.chat({'npc_name':'张三','message':'你还记得我的喜好吗？','player_id':'test'})
            self.assertIn('咖啡',result['message']);self.assertTrue(result['success'])
        finally:
            for h in other.log.handlers[:]: h.close();other.log.removeHandler(h)
    def test_memories_are_scoped_to_player_and_resident(self):
        self.chat('我喜欢草莓。')
        self.assertEqual([],self.service.history('王五','test'))
        self.assertEqual([],self.service.history('张三','different'))
        self.assertNotIn('草莓',self.chat('你记得吗？',player='different')['message'])
    def test_affinity_changes_and_bounds(self):
        self.assertEqual(24,self.chat('你好朋友')['affinity'])
        self.assertEqual(19,self.chat('笨蛋')['affinity'])
        for _ in range(30): self.chat('你好')
        self.assertEqual(100,self.service.get_affinity('张三','test'))
    def test_relevant_recall(self):
        self.chat('我喜欢蓝色和咖啡。')
        for i in range(12): self.chat('今天散步第'+str(i)+'次。')
        self.assertIn('咖啡',self.service.recall('张三','test','我喜欢什么颜色和咖啡')[0]['player'])
    def test_validation(self):
        for data in (None,[],{}, {'npc_name':[],'message':'x'}, {'npc_name':'张三','message':''}, {'npc_name':'张三','message':'x'*1001},{'npc_name':'王五','message':'a','player_id':'../secret'}):
            with self.assertRaises(ValueError):validate_chat(data)
    def test_real_provider_request_and_context(self):
        self.chat('我叫小林。')
        self.service.api_key='test-key'
        result=io.BytesIO(json.dumps({'choices':[{'message':{'content':'小林，我还记得你。'}}]},ensure_ascii=False).encode())
        with patch('urllib.request.urlopen',return_value=result) as call:
            response=self.chat('你记得我的名字吗？')
        request=call.call_args.args[0];body=json.loads(request.data)
        self.assertEqual('llm',response['source'])
        self.assertIn('小林',body['messages'][0]['content'])
        self.assertEqual('Bearer test-key',request.get_header('Authorization'))
    def test_provider_failure_does_not_save_fake_memory(self):
        self.service.api_key='secret'
        with patch('urllib.request.urlopen',side_effect=urllib.error.URLError('secret')):
            with self.assertRaises(ProviderError) as error:self.chat('你好')
        self.assertNotIn('secret',str(error.exception));self.assertEqual([],self.service.history('张三','test'))
    def test_qwen_non_thinking_request(self):
        self.service.api_key='test-key'
        self.service.base_url='https://maas.qianwenaiapi.com/compatible-mode/v1'
        self.service.model='qwen3.7-flash'
        self.service.enable_thinking=False
        result=io.BytesIO(json.dumps({'choices':[{'message':{'content':'你好。'}}]}).encode())
        with patch('urllib.request.urlopen',return_value=result) as call:
            self.assertEqual('llm',self.chat('你好')['source'])
        request=call.call_args.args[0]
        self.assertEqual('https://maas.qianwenaiapi.com/compatible-mode/v1/chat/completions',request.full_url)
        payload=json.loads(request.data)
        self.assertIs(False,payload['enable_thinking'])
        self.assertEqual('qwen3.7-flash',payload['model'])
    def test_generic_provider_omits_thinking_extension(self):
        self.service.api_key='test-key'
        self.service.enable_thinking=None
        result=io.BytesIO(json.dumps({'choices':[{'message':{'content':'Hello.'}}]}).encode())
        with patch('urllib.request.urlopen',return_value=result) as call:
            self.chat('Hello')
        self.assertNotIn('enable_thinking',json.loads(call.call_args.args[0].data))
    def test_english_language_prompt_and_demo(self):
        response=self.service.chat({'npc_name':'张三','message':'Hello','player_id':'english','language':'en'})
        self.assertIn('Welcome',response['message'])
        self.service.api_key='test-key'
        result=io.BytesIO(json.dumps({'choices':[{'message':{'content':'Hello, I am Zhang San.'}}]}).encode())
        with patch('urllib.request.urlopen',return_value=result) as call:
            self.service.chat({'npc_name':'张三','message':'Hello','player_id':'english','language':'en'})
        prompt=json.loads(call.call_args.args[0].data)['messages'][0]['content']
        self.assertIn('Reply only in natural, warm English',prompt)
        self.assertNotIn('用自然、温暖的中文回复',prompt)
        with self.assertRaises(ValueError):
            validate_chat({'npc_name':'张三','message':'Hello','language':[]})
    def test_http_routes_and_bad_request(self):
        server=ThreadingHTTPServer(('127.0.0.1',0),make_handler(self.service))
        thread=threading.Thread(target=server.serve_forever,daemon=True);thread.start()
        base='http://127.0.0.1:'+str(server.server_port)
        try:
            with urllib.request.urlopen(base+'/health') as r:self.assertEqual('demo',json.load(r)['mode'])
            payload=json.dumps({'npc_name':'王五','message':'你好','player_id':'api-test'}).encode()
            with urllib.request.urlopen(urllib.request.Request(base+'/chat',data=payload,headers={'Content-Type':'application/json'})) as r:self.assertTrue(json.load(r)['success'])
            with self.assertRaises(urllib.error.HTTPError) as error:urllib.request.urlopen(urllib.request.Request(base+'/chat',data=b'{}'))
            self.assertEqual(400,error.exception.code)
        finally:server.shutdown();server.server_close();thread.join()

if __name__=='__main__':unittest.main(verbosity=2)
