using System.Reflection;
using System.IO;
using UnityEngine.UI;
var g=UnityEngine.Object.FindFirstObjectByType<AgentTown.TownGame>();g.IsSmoke=true;g.Paused=true;
var r=g.Residents[0];var original=r.State.memories;
bool bounded=false,profile=false;
try
{
    r.State.memories=new List<AgentTown.Memory>();
    for(int i=0;i<100;i++)r.State.memories.Add(new AgentTown.Memory{time="第 1 天 09:00",source="本地演示",player=new string('甲',1000),reply=new string('乙',2000)});
    g.OpenChat(r);await Task.Delay(150);
    var transcript=(Text)typeof(AgentTown.TownUI).GetField("transcript",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(g.UI);
    bounded=transcript.text.Length<=12000&&transcript.cachedTextGenerator.vertexCount<65000;
    g.UI.ShowProfile(r);await Task.Delay(200);
    profile=UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None).All(t=>t.cachedTextGenerator.vertexCount<65000);
    g.UI.CloseTop();g.CloseChat();
}
finally {r.State.memories=original;}
var result=new{bounded,profile,storedRecords=100};File.WriteAllText("Builds/QA/long-conversation.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));
if(!bounded||!profile)throw new Exception("Long conversation test failed");
return result;
