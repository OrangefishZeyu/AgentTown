using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;
using UnityEngine.EventSystems;

namespace AgentTown
{
    public sealed class TownGame : MonoBehaviour
    {
        [NonSerialized] public TownWorld World;
        public TownSave Save;
        public TownPreferences Preferences;
        [NonSerialized] public List<Resident> Residents = new List<Resident>();
        public List<TownEvent> Events = new List<TownEvent>();
        public TownActor Player;
        public TownUI UI;
        public Camera Camera;
        [NonSerialized] public Resident Selected, Talking, WalkTo;
        public bool Paused, Busy;
        public float Speed = 1, Zoom = 1;
        public string Connection = "本地演示", Toast = "";
        public float ToastUntil;
        public bool IsSmoke;
        float saveTimer, thoughtTimer = 22, socialTimer=30, statusTimer=10, tintTimer;
        AudioSource ambience;
        public string Clock => $"{Mathf.FloorToInt(Save.minute / 60):00}:{Mathf.FloorToInt(Save.minute % 60):00}";
        public string Phase => Save.minute < 720 ? "上午" : Save.minute < 1080 ? "午后" : Save.minute < 1260 ? "黄昏" : "夜晚";
        public string SessionId => Preferences.playerId;
        static Color Hex(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }

        void Start()
        {
            IsSmoke=Array.IndexOf(Environment.GetCommandLineArgs(),"--smoke-test")>=0;
#if UNITY_EDITOR
            IsSmoke=IsSmoke||File.Exists("Tools/smoke-active.txt");
#endif
            var preview=GameObject.Find("AgentTown Preview"); if(preview) Destroy(preview);
            foreach(var cam in FindObjectsByType<Camera>(FindObjectsSortMode.None)) cam.gameObject.SetActive(false);
            Application.runInBackground=true; Application.targetFrameRate=60;
            Save=IsSmoke?new TownSave():TownStorage.Read<TownSave>(TownStorage.SavePath)??new TownSave();
            if(Save.version!=1) Save=new TownSave();
            Save.minute=Mathf.Repeat(Save.minute,1440);Save.day=Mathf.Max(1,Save.day);
            if(Save.residents==null)Save.residents=new List<ResidentState>();
            if(Save.visited==null)Save.visited=new List<string>();
            Preferences=TownStorage.Read<TownPreferences>(TownStorage.PreferencesPath)??new TownPreferences();
            TownLanguage.English=Preferences.english;
            if(string.IsNullOrEmpty(Preferences.playerId))Preferences.playerId=Guid.NewGuid().ToString("N");
            World=new TownWorld();World.Build();
            Camera=new GameObject("Town Camera").AddComponent<Camera>();Camera.orthographic=true;
            Camera.gameObject.AddComponent<AudioListener>();
            Camera.transform.position=new Vector3(0,0,-20);Camera.backgroundColor=Hex("#8cac7d");Camera.clearFlags=CameraClearFlags.SolidColor;
            Camera.orthographicSize=18; Camera.nearClipPlane=.1f;Camera.farClipPlane=100;
            Player=CreateActor("player",new Vector2(Save.playerX,Save.playerY));Player.Speed=4.1f;
            if(!World.Navigation.Walkable(Player.transform.position))Player.transform.position=TownNavigation.Center(World.Navigation.Nearest(Player.transform.position));
            AddResident("zhang","张三","Python 工程师","安静专注，喜欢分享技术，偶尔吐槽 bug","多智能体系统、Python、开源", "#7496a5", new Vector2(390,437),new Vector2(306,410));
            AddResident("li","李四","产品经理","热情健谈，关心每个人的想法","产品规划、需求分析、社区协作", "#bd8b7c",new Vector2(1008,444),new Vector2(1006,414));
            AddResident("wang","王五","UI 设计师","细腻温柔，喜欢观察光影和喝咖啡","界面设计、配色、交互体验", "#c5a76d",new Vector2(347,852),new Vector2(938,414));
            Selected=Residents[0];
            UI=gameObject.AddComponent<TownUI>(); UI.Build(this);
            AddEvent("欢迎来到 AgentTown", "这里的每个人，都有自己的故事。先和邻居打个招呼吧。",Hex("#719b84"));
            CreateAmbience();
            if(Preferences.useBackend)StartCoroutine(CheckBackend());
            if(IsSmoke)StartCoroutine(SmokeTest());
        }
        TownActor CreateActor(string art, Vector2 p)
        {
            var obj=new GameObject(art=="player"?"Player · 旅人":art);obj.transform.position=p;
            var actor=obj.AddComponent<TownActor>();actor.Initialize(art);return actor;
        }
        void AddResident(string id,string name,string role,string personality,string expertise,string color,Vector2 initial,Vector2 home)
        {
            ResidentState state=Save.residents.Find(x=>x.id==id);
            if(state==null) {state=new ResidentState{id=id,x=TownNavigation.FromPixel(initial.x,initial.y).x,y=TownNavigation.FromPixel(initial.x,initial.y).y};Save.residents.Add(state);}
            state.affinity=Mathf.Clamp(state.affinity,0,100);
            if(state.memories==null)state.memories=new List<Memory>();
            var r=new Resident{Id=id,Name=name,Role=role,Personality=personality,Expertise=expertise,Sprite=id,Color=Hex(color),Home=TownNavigation.FromPixel(home.x,home.y),State=state,DecisionTimer=12+Residents.Count*7,Location=id=="wang"?"半日咖啡":"工坊前的小路",Activity=id=="wang"?"享用早晨的咖啡":"准备今天的工作",Thought=id=="zhang"?"今天也要让代码优雅一点。":id=="li"?"好的产品，始于一次认真倾听。":"这片树影，很适合做新的配色。"};
            Vector2 pos=new Vector2(state.x,state.y);if(!World.Navigation.Walkable(pos))pos=TownNavigation.Center(World.Navigation.Nearest(pos));
            r.Actor=CreateActor(id,pos);r.Actor.Speed=1.4f+Residents.Count*.12f;Residents.Add(r);
        }
        void Update()
        {
            if(UI==null)return;
            float dt=Time.deltaTime;
            UpdateCamera();
            bool manual=!UI.ModalOpen&&!UI.InputFocused&&MovementInput().sqrMagnitude>0;
            if(!manual)Player.Tick(dt);
            HandleInput(dt);
            if(WalkTo!=null && !UI.ModalOpen)
            {
                if(Vector2.Distance(Player.transform.position,WalkTo.Actor.transform.position)<2.3f){var r=WalkTo;WalkTo=null;OpenChat(r);}
                else if(!Player.Moving) Player.Go(World.Navigation.FindPath(Player.transform.position,WalkTo.Actor.transform.position));
            }
            if(!Paused)
            {
                Save.minute+=dt*Speed*2;
                if(Save.minute>=1440){Save.minute-=1440;Save.day++;AddEvent("新的一天", "昨夜的故事，成为今天的记忆。",Hex("#719b84"));}
                foreach(var r in Residents)TickResident(r,dt*Speed);
                thoughtTimer-=dt*Speed; if(thoughtTimer<0){thoughtTimer=22;Think();}
                socialTimer-=dt*Speed;if(socialTimer<0){socialTimer=24;Socialize();}
            }
            foreach(var r in Residents)r.BubbleTimer=Mathf.Max(0,r.BubbleTimer-dt);
            saveTimer+=dt;if(saveTimer>30){saveTimer=0;SaveGame(false);}
            statusTimer-=dt;if(statusTimer<0 && Preferences.useBackend){statusTimer=60;StartCoroutine(PollThoughts());}
            tintTimer-=dt;if(tintTimer<0){tintTimer=2;UpdateTint();}
            CheckExploration(); UI.Refresh();
        }
        void UpdateCamera()
        {
            float scale=Screen.height/1000f;
            float width=Mathf.Max(200,Screen.width-344*scale),height=Screen.height-154*scale;
            Camera.rect=new Rect(0,62*scale/Screen.height,width/Screen.width,height/Screen.height);
            float fit=Mathf.Max(17.1f,25.2f/(width/height));
            Camera.orthographicSize=Mathf.Lerp(Camera.orthographicSize,fit/Zoom,Time.deltaTime*7);
            Vector2 focus=Zoom>1.15f?(Vector2)Player.transform.position:Vector2.zero;
            Vector3 target=new Vector3(Mathf.Clamp(focus.x,-8,8),Mathf.Clamp(focus.y,-4,4),-20);
            Camera.transform.position=Vector3.Lerp(Camera.transform.position,target,Time.deltaTime*5);
        }
        void HandleInput(float dt)
        {
            var k=Keyboard.current;var mouse=Mouse.current;
            if(k!=null&&k.escapeKey.wasPressedThisFrame){UI.CloseTop();return;}
            if(UI.ModalOpen||UI.InputFocused)return;
            if(k!=null)
            {
                if(k.spaceKey.wasPressedThisFrame)Paused=!Paused;
                if(k.f5Key.wasPressedThisFrame)SaveGame(true);
                if(k.eKey.wasPressedThisFrame){var r=NearestResident();if(r!=null)OpenChat(r);else Notify("靠近居民后按 E，或从右侧选择「前往交谈」。");}
                Vector2 movement=MovementInput();
                if(movement.sqrMagnitude>0)
                {
                    WalkTo=null;Player.Path.Clear();movement=movement.normalized*Player.Speed*dt;
                    Vector2 pos=Player.transform.position,next=pos;
                    if(World.Navigation.Walkable(pos+new Vector2(movement.x,0)))next.x+=movement.x;
                    if(World.Navigation.Walkable(next+new Vector2(0,movement.y)))next.y+=movement.y;
                    Player.transform.position=next;Player.Animate(next-pos,dt);
                }
            }
            if(mouse!=null&&!EventSystem.current.IsPointerOverGameObject())
            {
                Vector2 p=mouse.position.ReadValue();
                if(Camera.pixelRect.Contains(p))
                {
                    if(mouse.scroll.ReadValue().y!=0)Zoom=Mathf.Clamp(Zoom+mouse.scroll.ReadValue().y*.0016f,1,2.3f);
                    if(mouse.leftButton.wasPressedThisFrame)
                    {
                        Vector2 world=Camera.ScreenToWorldPoint(p);
                        var r=Residents.Find(x=>Vector2.Distance(world,(Vector2)x.Actor.transform.position+Vector2.up*.6f)<1);
                        if(r!=null)GoTalk(r);
                        else {WalkTo=null;Player.Go(World.Navigation.FindPath(Player.transform.position,world));UI.SetWaypoint(world);}
                    }
                }
            }
        }
        static Vector2 MovementInput()
        {
            var k=Keyboard.current;if(k==null)return Vector2.zero;
            return new Vector2((k.dKey.isPressed||k.rightArrowKey.isPressed?1:0)-(k.aKey.isPressed||k.leftArrowKey.isPressed?1:0),(k.wKey.isPressed||k.upArrowKey.isPressed?1:0)-(k.sKey.isPressed||k.downArrowKey.isPressed?1:0));
        }
        void TickResident(Resident r,float dt)
        {
            r.Actor.Held=Talking==r;
            r.State.energy=Mathf.Clamp(r.State.energy+dt*(Save.minute>1260||Save.minute<420?.05f:-.008f),20,100);
            r.Actor.Tick(dt);
            if(Talking==r)return;
            r.DecisionTimer-=dt;
            if(r.DecisionTimer>0)return;
            r.DecisionTimer=UnityEngine.Random.Range(20,35);
            float h=Save.minute/60;
            int schedule=h<7?0:h<9?1:h<12?2:h<14?3:h<17?4:h<20?5:h<22?6:0;
            Vector2 target=r.Home;
            switch(schedule)
            {
                case 0:r.Activity="安静休息";r.Location="家门口";break;
                case 1:case 3:r.Activity=schedule==1?"吃早餐，补充灵感":"午间休息";r.Location="半日咖啡";target=TownNavigation.FromPixel(306,850);break;
                case 2:r.Activity=r.Id=="zhang"?"调试多智能体系统":r.Id=="li"?"整理小镇居民的需求":"绘制新的界面草图";r.Location=r.Id=="zhang"?"智能体工坊":"共创工作室";break;
                case 4:r.Activity=r.Id=="li"?"翻阅社区活动资料":"专注今天的创作";r.Location=r.Id=="li"?"小镇图书馆":r.Id=="zhang"?"智能体工坊":"共创工作室";if(r.Id=="li")target=TownNavigation.FromPixel(1000,872);break;
                case 5:r.Activity="在喷泉广场散步";r.Location="喷泉广场";target=TownNavigation.FromPixel(650+Residents.IndexOf(r)*100,625);break;
                case 6:r.Activity=r.Id=="wang"?"收集黄昏的颜色":"和邻居聊聊今天";r.Location="半日咖啡";target=TownNavigation.FromPixel(340+Residents.IndexOf(r)*55,856);break;
            }
            Vector2 jitter=new Vector2(UnityEngine.Random.Range(-1.2f,1.2f),UnityEngine.Random.Range(-.3f,.7f));
            r.Destination=target+jitter;r.Actor.Go(World.Navigation.FindPath(r.Actor.transform.position,r.Destination));
            if(r.Schedule!=schedule){r.Schedule=schedule;AddEvent(r.Name+" · "+r.Location,r.Activity,r.Color);}
        }
        void Think()
        {
            Resident r=Residents[UnityEngine.Random.Range(0,Residents.Count)];
            if(r==Talking)return;
            string[][] lines={new[]{"这个 bug，终于有思路了。","代码之外，也要看看天空。","想把小镇的记忆连成一张图。","先写一个小测试，再喝咖啡。"},new[]{"如果这里能办个分享会就好了。","想听听大家对小镇的期待。","一杯咖啡的时间，聊一个好点子。","今天又收集到了新想法。"},new[]{"鼠尾草绿，配一点暖杏色。","午后的光落在屋顶上，真好看。","想给咖啡馆画一张新菜单。","设计里也需要一点留白。"}};
            int i=Residents.IndexOf(r);r.Thought=lines[i][UnityEngine.Random.Range(0,4)];r.Bubble=r.Thought;r.BubbleTimer=6;
            AddEvent(r.Name+" 的片刻想法",r.Thought,r.Color);
        }
        void Socialize()
        {
            for(int i=0;i<Residents.Count;i++)for(int j=i+1;j<Residents.Count;j++)
            {
                var a=Residents[i];var b=Residents[j];
                if(a==Talking||b==Talking||Vector2.Distance(a.Actor.transform.position,b.Actor.transform.position)>4)continue;
                Save.socialEvents++;a.Bubble=$"{b.Name}，晚点一起喝杯咖啡？";a.BubbleTimer=7;
                b.Bubble="好呀，也想听听你的新想法。";b.BubbleTimer=7;
                a.State.mood=Mathf.Min(100,a.State.mood+2);b.State.mood=Mathf.Min(100,b.State.mood+2);
                AddEvent(a.Name+" 与 "+b.Name+" 偶遇","交换了今天的灵感，约好晚点再聊。",a.Color);return;
            }
        }
        public Resident NearestResident()
        {
            Resident nearest=null;float distance=2.6f;
            foreach(var r in Residents){float d=Vector2.Distance(Player.transform.position,r.Actor.transform.position);if(d<distance){nearest=r;distance=d;}}
            return nearest;
        }
        public void GoTalk(Resident r)
        {
            Selected=r;WalkTo=r;
            Player.Go(World.Navigation.FindPath(Player.transform.position,r.Actor.transform.position));
            Notify("正在前往 "+r.Name+" 身边…");
        }
        public void OpenChat(Resident r)
        {
            if(Busy)return;
            if(Talking!=null)Talking.Actor.Held=false;
            Selected=r;Talking=r;Player.Path.Clear();WalkTo=null;r.Actor.Held=true;UI.ShowChat(r);
        }
        public void CloseChat(){if(Talking!=null)Talking.Actor.Held=false;Talking=null;UI.HideChat();}
        public void Send(string message)
        {
            if(Busy||Talking==null||string.IsNullOrWhiteSpace(message))return;
            message=message.Trim();if(message.Length>1000){Notify("每条消息最多 1000 字。");return;}
            StartCoroutine(Chat(Talking,message));
        }
        IEnumerator Chat(Resident r,string message)
        {
            Busy=true;UI.BeginMessage(message);
            string reply=null,source="本地演示";float delta=AffinityDelta(message);
            if(Preferences.useBackend)
            {
                var body=new ChatRequest{npc_name=r.Name,message=message,player_id=SessionId,location=r.Location,activity=r.Activity,game_time=$"第 {Save.day} 天 {Clock}",affinity=r.State.affinity,language=Preferences.english?"en":"zh"};
                using(var request=Post("/chat",JsonUtility.ToJson(body)))
                {
                    yield return request.SendWebRequest();
                    if(request.result==UnityWebRequest.Result.Success)
                    {
                        ChatReply result=null;try{result=JsonUtility.FromJson<ChatReply>(request.downloadHandler.text);}catch(Exception){ }
                        if(result!=null&&result.success&&!string.IsNullOrEmpty(result.message))
                        {reply=result.message;source=result.source=="llm"?"AI · 长期记忆":"后端演示";delta=Mathf.Clamp(result.affinity_delta,-10,6);Connection=result.source=="llm"?"AI 已连接":"后端演示";}
                        else Notify("后端未返回有效内容，已使用本地演示回复。");
                    }
                    else {Connection="后端不可用";Notify("后端连接失败，已使用本地演示回复。请检查设置。 ");}
                }
            }
            if(reply==null){yield return new WaitForSecondsRealtime(.65f);reply=LocalReply(r,message);source="本地演示";}
            r.State.affinity=Mathf.Clamp(r.State.affinity+delta,0,100);r.State.met=true;
            r.State.mood=Mathf.Clamp(r.State.mood+delta,0,100);
            r.State.memories.Add(new Memory{time=$"第 {Save.day} 天 {Clock}",player=message,reply=reply,source=source,importance=message.Contains("喜欢")||message.Contains("叫")?3:1});
            while(r.State.memories.Count>100)r.State.memories.RemoveAt(0);
            Save.conversations++;r.Thought=reply;r.Bubble=reply.Length>24?reply.Substring(0,24)+"…":reply;r.BubbleTimer=7;
            AddEvent("你与 "+r.Name+" 的对话",$"记住了一段新故事 · 好感 {(delta>=0?"+":"")}{delta:0}",r.Color);
            Busy=false;UI.EndMessage(r,reply,source,delta);CheckRewards();SaveGame(false);
        }
        public static float AffinityDelta(string m)
        {
            foreach(string bad in new[]{"讨厌你","滚开","笨蛋","垃圾","去死"})if(m.Contains(bad))return -5;
            foreach(string good in new[]{"谢谢","喜欢","帮助","朋友","一起","你好","辛苦"})if(m.Contains(good))return 4;
            return 2;
        }
        public static string LocalReply(Resident r,string m)
        {
            if(TownLanguage.English)
            {
                if(m.ToLowerInvariant().Contains("remember")&&r.State.memories.Count>0)
                    return "I remember you saying: “"+r.State.memories[r.State.memories.Count-1].player+"”. What would you like to talk about today?";
                return "I'm "+TownLanguage.Text(r.Name)+", the town's "+TownLanguage.Text(r.Role)+". "+(r.Id=="zhang"?"I've been working on how agents remember things. What interests you about that?":r.Id=="li"?"I enjoy hearing people's ideas for our community. What's on your mind?":"I find inspiration in light, colors and everyday life. What caught your eye today?");
            }
            string lower=m.ToLowerInvariant();
            if(m.Contains("记得")||m.Contains("记忆")||m.Contains("我叫什么"))
            {
                for(int i=r.State.memories.Count-1;i>=0;i--)
                {
                    string text=r.State.memories[i].player;
                    if(text.Contains("我叫")||text.Contains("喜欢")||text.Contains("名字"))return "记得呀，你之前说过「"+text+"」。我把这些小事都记在心里了。";
                }
                return r.State.memories.Count>0?"当然记得。我们上次聊到「"+r.State.memories[r.State.memories.Count-1].player+"」。今天有新的故事吗？":"我们还没有聊过太多，不过从现在开始，你分享的小事我会认真记住。";
            }
            if(m.Contains("我叫"))return "很高兴认识你！我会记住这个名字。我是"+r.Name+"，在小镇做"+r.Role+"，以后常来坐坐。";
            if(m.Contains("喜欢"))return "我记下了：你说「"+m+"」。了解一个人，就是从这些小小的喜好开始的。";
            if(m.Contains("你好")||m.Contains("认识"))return "你好，我是"+r.Name+"，一名"+r.Role+"。"+(r.Id=="zhang"?"刚好从代码里抬起头，和你聊聊天。":r.Id=="li"?"欢迎你来小镇，有什么新想法都可以告诉我。":"刚刚在看屋顶的颜色。很高兴在这里遇见你。");
            if(m.Contains("咖啡")||m.Contains("一起"))return r.Id=="wang"?"好呀！半日咖啡的靠窗位置能看到树影，我最喜欢那儿。等今天的草图画完，我们一起去。":"听起来不错！忙完手头的事，我也想去半日咖啡坐坐。那里总能碰见有趣的邻居。";
            if(m.Contains("谢谢"))return "不客气。能帮到你，我也很开心。小镇就是因为这些互相照应的小事，才慢慢变得温暖。";
            if(AffinityDelta(m)<0)return "听到这句话，我有些难过。我们可以慢慢聊，也希望彼此多一点尊重。";
            if(lower.Contains("python")||lower.Contains("agent")||m.Contains("代码")||m.Contains("技术"))return r.Id=="zhang"?"我会先把智能体拆成感知、记忆、规划和行动，再用小测试验证每一步。你想先聊哪个部分？":"技术方面可以找张三聊聊。他在智能体工坊，对多智能体和 Python 都很熟悉。";
            if(m.Contains("设计")||m.Contains("配色"))return r.Id=="wang"?"我想用鼠尾草绿搭配奶油色，让界面像这个小镇一样安静。先把信息排清楚，再留一点呼吸的空间。":"王五正在画新草图，可以去共创工作室找他。比起堆满元素，他更喜欢舒服的留白。";
            if(m.Contains("工作")||m.Contains("做什么")||m.Contains("计划"))return "我正在"+r.Activity+"。"+(r.Id=="zhang"?"希望让每个智能体都能记住重要的事。":r.Id=="li"?"我想把大家的期待变成真正能用的功能。":"灵感常常来自生活里不起眼的细节。")+"你今天有什么计划？";
            if(m.Contains("小镇")||m.Contains("哪里"))return "北边是智能体工坊和共创工作室，南边有半日咖啡与图书馆。傍晚大家常在喷泉广场碰面，去河边散步也很舒服。";
            if(m.Contains("朋友"))return r.State.affinity>=40?"当然，我们已经一起分享了不少故事。以后有开心的事，或者烦恼，都可以来找我。":"我很愿意慢慢了解你。友谊大概就像小镇里的树，需要一点时间，也需要经常照顾。";
            string[] fallback={"我听见了。站在"+r.Role+"的角度，我也很想知道，你最在意这件事的哪一部分？","这让我想起今天在"+r.Location+"观察到的小事。你愿意再多讲一点吗？","和你聊天总能发现新的角度。等忙完"+r.Activity+"，我想继续听听你的想法。"};
            return fallback[r.State.memories.Count%fallback.Length];
        }
        public UnityWebRequest Post(string route,string json)
        {
            var request=new UnityWebRequest(Preferences.endpoint.TrimEnd('/')+route,"POST");
            request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));request.downloadHandler=new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type","application/json");request.timeout=35;return request;
        }
        public IEnumerator CheckBackend()
        {
            Connection="连接中…";
            using(var request=UnityWebRequest.Get(Preferences.endpoint.TrimEnd('/')+"/health"))
            {
                request.timeout=5;yield return request.SendWebRequest();
                HealthReply result=null;
                if(request.result==UnityWebRequest.Result.Success)try{result=JsonUtility.FromJson<HealthReply>(request.downloadHandler.text);}catch(Exception){ }
                Connection=result!=null&&result.status=="healthy"?(result.mode=="llm"?"AI 已连接":"后端演示"):"后端不可用";
                Notify(Connection+(result!=null&&result.mode=="demo"?" · 请在 Backend/.env 配置模型 Key":""));
            }
        }
        IEnumerator PollThoughts()
        {
            using(var request=UnityWebRequest.Get(Preferences.endpoint.TrimEnd('/')+"/npcs/status"))
            {
                request.timeout=5;yield return request.SendWebRequest();
                if(request.result!=UnityWebRequest.Result.Success)yield break;
                StatusReply state=null;try{state=JsonUtility.FromJson<StatusReply>(request.downloadHandler.text);}catch(Exception){ }
                if(state?.residents!=null)foreach(var item in state.residents){var r=Residents.Find(x=>x.Name==item.name);if(r!=null&&!string.IsNullOrEmpty(item.thought))r.Thought=item.thought;}
            }
        }
        void CheckExploration()
        {
            string[] names={"智能体工坊","共创工作室","半日咖啡","小镇图书馆"};
            Vector2[] doors={TownNavigation.FromPixel(306,408),TownNavigation.FromPixel(1006,408),TownNavigation.FromPixel(306,850),TownNavigation.FromPixel(1006,873)};
            for(int i=0;i<4;i++)if(!Save.visited.Contains(names[i])&&Vector2.Distance(Player.transform.position,doors[i])<2)
            {Save.visited.Add(names[i]);Notify("发现地点 · "+names[i]);AddEvent("新的足迹", "你来到了"+names[i]+"。",Hex("#96aa80"));}
        }
        void CheckRewards()
        {
            if(!Save.introductionReward&&Residents.TrueForAll(x=>x.State.met)) {Save.introductionReward=true;foreach(var r in Residents)r.State.affinity=Mathf.Min(100,r.State.affinity+3);Notify("心愿完成 · 认识了所有邻居！全员好感 +3");}
            if(!Save.friendshipReward&&Residents.Exists(x=>x.State.affinity>=40)){Save.friendshipReward=true;Notify("心愿完成 · 收获第一段友谊。");}
        }
        public void AddEvent(string title,string text,Color color)
        {Events.Insert(0,new TownEvent{time=Clock,title=title,text=text,color=color});if(Events.Count>60)Events.RemoveAt(Events.Count-1);}
        public void Notify(string text){Toast=text;ToastUntil=Time.unscaledTime+4;}
        public void ChangeLanguage()
        {
            if(Busy)return;
            Preferences.english=!Preferences.english;
            SaveGame(false);
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
        public void NewGame()
        {
            if(Busy||IsSmoke)return;
            SaveGame(false);
            string archive=Path.Combine(TownStorage.DirectoryPath,"Archives",DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(archive);
            File.Copy(TownStorage.SavePath,Path.Combine(archive,"town-save.json"));
            File.Copy(TownStorage.PreferencesPath,Path.Combine(archive,"preferences.json"));
            Preferences.playerId=Guid.NewGuid().ToString("N");
            Save=new TownSave();
            TownStorage.AtomicWrite(TownStorage.SavePath,JsonUtility.ToJson(Save,true));
            TownStorage.AtomicWrite(TownStorage.PreferencesPath,JsonUtility.ToJson(Preferences,true));
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }
        public void SaveGame(bool notify)
        {
            if(IsSmoke)return;
            Save.playerX=Player.transform.position.x;Save.playerY=Player.transform.position.y;
            foreach(var r in Residents){r.State.x=r.Actor.transform.position.x;r.State.y=r.Actor.transform.position.y;}
            try{TownStorage.AtomicWrite(TownStorage.SavePath,JsonUtility.ToJson(Save,true));TownStorage.AtomicWrite(TownStorage.PreferencesPath,JsonUtility.ToJson(Preferences,true));if(notify)Notify("已保存 · "+Clock);}
            catch(Exception ex){Debug.LogError("保存失败："+ex);Notify("存档写入失败，请检查磁盘空间。");}
        }
        void OnApplicationQuit(){if(Player!=null)SaveGame(false);}
        void OnApplicationPause(bool paused){if(paused&&Player!=null)SaveGame(false);}
        void UpdateTint()
        {
            float h=Save.minute/60;Color tint=h>=20||h<6?new Color(.57f,.66f,.79f):h>=18?new Color(1,.85f,.73f):h<8?new Color(.93f,.94f,.86f):Color.white;
            foreach(var sr in World.Root.GetComponentsInChildren<SpriteRenderer>())sr.color=tint;
            foreach(var r in Residents)r.Actor.Body.color=tint;Player.Body.color=tint;
            Camera.backgroundColor=Hex("#8cac7d")*tint;
        }
        void CreateAmbience()
        {
            const int rate=22050,seconds=32;var data=new float[rate*seconds];
            float[] notes={261.63f,329.63f,392,440,392,329.63f,293.66f,261.63f,329.63f,392,523.25f,440,392,329.63f,293.66f,0};
            for(int i=0;i<data.Length;i++){float t=i/(float)rate, local=t%2, note=notes[(int)(t/2)%notes.Length];float env=Mathf.Exp(-local*2.1f)*Mathf.Min(1,local*40);data[i]=note>0?Mathf.Sin(t*note*2*Mathf.PI)*env*.045f+Mathf.Sin(t*note*Mathf.PI)*env*.018f:0;}
            var clip=AudioClip.Create("Small hours · procedural",data.Length,1,rate,false);clip.SetData(data,0);
            ambience=gameObject.AddComponent<AudioSource>();ambience.clip=clip;ambience.loop=true;ambience.volume=.35f;ambience.Play();ambience.mute=!Preferences.sound;
        }
        public void SetSound(bool on){Preferences.sound=on;if(ambience)ambience.mute=!on;}
        IEnumerator SmokeTest()
        {
            yield return new WaitForSeconds(2);Paused=true;
            var r=Residents[0];var path=World.Navigation.FindPath(Player.transform.position,r.Actor.transform.position);
            if(path.Count==0)throw new Exception("Smoke: path to resident is missing");
            Player.Go(path);float timeout=35;
            while(Player.Moving&&timeout>0){timeout-=Time.deltaTime;yield return null;}
            if(timeout<=0)throw new Exception("Smoke: movement timed out");
            OpenChat(r);Send("你好，我叫小林，喜欢咖啡。");while(Busy)yield return null;
            Send("你记得我喜欢什么吗？");while(Busy)yield return null;
            bool memory=r.State.memories.Count==2&&r.State.memories[1].reply.Contains("咖啡");
            string folder=Path.Combine(Application.dataPath,Application.isEditor?"../Builds/QA":"../QA");Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"runtime-smoke.json"),"{\"movement\":true,\"memory\":"+memory.ToString().ToLowerInvariant()+",\"conversations\":"+Save.conversations+",\"affinity\":"+r.State.affinity+"}");
            ScreenCapture.CaptureScreenshot(Path.Combine(folder,"dialogue.png"));yield return new WaitForSeconds(1);
            CloseChat();ScreenCapture.CaptureScreenshot(Path.Combine(folder,"town.png"));yield return new WaitForSeconds(1);
            Debug.Log("AGENTTOWN_SMOKE_"+(memory?"PASS":"FAIL"));
#if UNITY_EDITOR
            if(File.Exists("Tools/smoke-active.txt"))File.Delete("Tools/smoke-active.txt");
            enabled=false;
            UnityEditor.EditorApplication.delayCall+=()=>UnityEditor.EditorApplication.isPlaying=false;
#else
            Application.Quit(memory?0:1);
#endif
        }
    }
}
