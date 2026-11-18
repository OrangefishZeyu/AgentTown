using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace AgentTown
{
    public sealed class TownUI : MonoBehaviour
    {
        TownGame game;
        Canvas canvas;
        RectTransform root,side,chat,modal,veil;
        Font font;
        Sprite round;
        Text clock,phase,connection,pause,toast,detail,taskHint,chatTitle,chatSource,transcript,sendLabel,status,helpHint;
        InputField input,endpoint;
        ScrollRect chatScroll;
        GameObject chatObject,modalObject,toastObject,sideContent;
        readonly List<Text> cardText=new List<Text>(),worldNames=new List<Text>(),bubbles=new List<Text>(),signs=new List<Text>();
        readonly List<Image> cardBars=new List<Image>();
        Text playerLabel,waypoint;
        Vector2 waypointPosition;
        float waypointUntil,sideRefresh;
        int tab;
        bool memoryView;
        Color Ink=H("#314640"),Muted=H("#829087"),Green=H("#688e77"),Cream=H("#f4f2e8"),Line=H("#dedfd3");
        public bool ModalOpen => (chatObject&&chatObject.activeSelf)||(modalObject&&modalObject.activeSelf);
        public bool InputFocused => (input&&input.isFocused)||(endpoint&&endpoint.isFocused);
        float Width => Screen.width/(Screen.height/1000f);
        float WorldWidth => Width-344;
        static Color H(string html){ColorUtility.TryParseHtmlString(html,out var c);return c;}
        static string Escape(string text)=>text?.Replace("<","‹").Replace(">","›")??"";
        static string Preview(string text,int max)=>text.Length<=max?text:text.Substring(0,max)+"…";
        void AppendTranscript(string text)
        {
            string value=transcript.text+text;
            if(value.Length>12000)
            {
                int start=value.IndexOf("\n\n",value.Length-11000,StringComparison.Ordinal);
                value=start>=0?value.Substring(start+2):value.Substring(value.Length-11000);
            }
            transcript.text=value;
        }

        public void Build(TownGame owner)
        {
            game=owner;
            font=Font.CreateDynamicFontFromOSFont(new[]{"Microsoft YaHei","Microsoft YaHei UI","Noto Sans CJK SC","PingFang SC","Arial"},18);
            if(!font)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            round=MakeRounded();
            var obj=new GameObject("Town UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas=obj.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=10;
            var scaler=obj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1600,1000);scaler.matchWidthOrHeight=1;
            root=obj.GetComponent<RectTransform>();
            if(!EventSystem.current){var es=new GameObject("Event System",typeof(EventSystem),typeof(InputSystemUIInputModule));es.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();}
            BuildHeader();BuildFooter();BuildSide();BuildWorldLabels();BuildChat();
            toastObject=Panel(root,"Toast",new Rect(28,864,700,48),H("#344c43")).gameObject;toastObject.SetActive(false);
            toast=Label(toastObject.transform,"",18,16,668,24,15,H("#ffffff"));
            taskHint=Label(root,"",28,808,520,42,15,Ink);taskHint.alignment=TextAnchor.MiddleLeft;
            var mapTag=Panel(root,"Map title",new Rect(28,118,262,69),new Color(.97f,.96f,.90f,.94f));
            Label(mapTag,"暮光溪谷",16,9,160,26,21,Ink,FontStyle.Bold);
            Label(mapTag,"一座会记住你的 AI 小镇",16,39,232,20,12,Muted);
            var zoomBox=Panel(root,"Map tools",new Rect(28,205,44,142),new Color(.97f,.96f,.90f,.95f));
            Button(zoomBox,"+",7,6,30,36,()=>game.Zoom=Mathf.Min(2.3f,game.Zoom+.25f),Cream,Ink,23);
            Button(zoomBox,"−",7,53,30,36,()=>game.Zoom=Mathf.Max(1,game.Zoom-.25f),Cream,Ink,23);
            Button(zoomBox,"◎",7,99,30,34,()=>game.Zoom=1,Cream,Ink,21);
        }
        void BuildHeader()
        {
            var bar=Panel(root,"Header",new Rect(0,0,0,92),Cream);StretchX(bar,92,true);
            var mark=Panel(bar,"Brand",new Rect(26,24,44,44),Green);
            Label(mark,"a",9,0,28,41,32,H("#f7f4e9"),FontStyle.Bold);
            Label(bar,"AGENTTOWN",86,18,260,31,25,Ink,FontStyle.Bold);
            Label(bar,"赛博小镇  /  每一次相遇，都留下记忆",87,53,366,19,12,Muted);
            var right=Panel(bar,"Header controls",new Rect(0,0,790,92),Color.clear,false);AnchorRight(right,0,0);
            clock=Label(right,"",18,23,170,31,20,Ink,FontStyle.Bold);
            phase=Label(right,"",18,55,170,19,12,Muted);
            pause=Button(right,"Ⅱ",190,24,42,42,()=>game.Paused=!game.Paused,H("#e4e8dc"),Ink,20).GetComponentInChildren<Text>();
            Button(right,"1×",242,24,47,42,()=>{game.Speed=1;game.Paused=false;},H("#e4e8dc"),Ink,14);
            Button(right,"3×",298,24,47,42,()=>{game.Speed=3;game.Paused=false;},H("#e4e8dc"),Ink,14);
            Button(right,"6×",354,24,47,42,()=>{game.Speed=6;game.Paused=false;},H("#e4e8dc"),Ink,14);
            connection=Button(right,"●  本地演示",425,24,166,42,ShowSettings,H("#e7ece2"),Green,14).GetComponentInChildren<Text>();
            Button(right,"保存",610,24,64,42,()=>game.SaveGame(true),H("#e7e7dc"),Ink,14);
            Button(right,"设置",686,24,76,42,ShowSettings,H("#344c43"),Cream,14);
            var line=Panel(bar,"Divider",new Rect(0,91,0,1),Line,false);StretchX(line,1,true);line.anchoredPosition=new Vector2(0,-91);
        }
        void BuildFooter()
        {
            var footer=Panel(root,"Footer",new Rect(0,0,0,62),Cream);StretchX(footer,62,false);
            Label(footer,"WASD  移动     点击地面  寻路     E  交谈     滚轮  缩放     空格  暂停",28,21,865,22,13,Muted);
            var area=Panel(footer,"Footer right",new Rect(0,0,334,62),Color.clear,false);AnchorRight(area,0,0);
            Button(area,"玩法说明",12,13,98,36,ShowHelp,H("#e5e8dd"),Ink,13);
            Button(area,"声音 开/关",120,13,112,36,()=>{game.SetSound(!game.Preferences.sound);game.Notify(game.Preferences.sound?"已开启环境音乐":"已关闭环境音乐");},H("#e5e8dd"),Ink,13);
            Label(area,"v1.0",249,22,64,20,12,Muted);
        }
        void BuildSide()
        {
            side=Panel(root,"Residents sidebar",new Rect(0,92,344,846),Cream);AnchorRight(side,0,92);
            Label(side,"小镇手记",26,25,230,32,24,Ink,FontStyle.Bold);
            Label(side,"03 位居民   /   正在发生的生活",26,66,288,22,12,Muted);
            for(int i=0;i<3;i++)
            {
                int t=i;Button(side,new[]{"居民","动态","心愿"}[i],26+i*100,104,92,38,()=>{tab=t;RenderSide();},H("#e4e8dc"),Ink,14);
            }
            RenderSide();
            Label(side,"●  每 30 秒自动保存",28,788,290,21,12,Green);
            Label(side,"慢慢走，故事会自己发生。",28,814,290,20,12,Muted);
        }
        void RenderSide()
        {
            if(sideContent)Destroy(sideContent);
            sideContent=new GameObject("Sidebar content",typeof(RectTransform));sideContent.transform.SetParent(side,false);
            var container=sideContent.GetComponent<RectTransform>();SetRect(container,new Rect(0,162,344,610));
            cardText.Clear();cardBars.Clear();detail=null;
            if(tab==0)
            {
                for(int i=0;i<game.Residents.Count;i++)
                {
                    var r=game.Residents[i];
                    var card=Button(container,"",22,i*117,300,106,()=>{game.Selected=r;},H("#e9ebdf"),Ink,14).GetComponent<RectTransform>();
                    Portrait(card,r,13,14,52);
                    Label(card,r.Name,79,12,110,27,20,Ink,FontStyle.Bold);
                    Label(card,r.Role,184,18,112,21,12,Muted);
                    cardText.Add(Label(card,"",79,44,203,22,12,Muted));
                    var track=Panel(card,"Relationship track",new Rect(79,80,198,4),H("#d5dace"),false);
                    cardBars.Add(Panel(track,"Value",new Rect(0,0,50,4),r.Color,false).GetComponent<Image>());
                }
                var info=Panel(container,"Selected resident",new Rect(22,359,300,220),H("#e8eadf"));
                Label(info,"此刻的故事",16,14,240,24,14,Green,FontStyle.Bold);
                detail=Label(info,"",16,47,266,110,14,Ink);
                Button(info,"前往交谈  →",16,166,157,38,()=>game.GoTalk(game.Selected),Green,Cream,14);
                Button(info,"档案",186,166,96,38,()=>ShowProfile(game.Selected),H("#dce2d3"),Ink,14);
            }
            else if(tab==1)
            {
                int max=Mathf.Min(7,game.Events.Count);
                for(int i=0;i<max;i++)
                {
                    var e=game.Events[i];float y=i*85;
                    Label(container,e.time,25,y+2,54,20,11,Muted);
                    Panel(container,"Timeline dot",new Rect(84,y+7,6,6),e.color,false);
                    Label(container,e.title,104,y,215,24,14,Ink,FontStyle.Bold);
                    Label(container,e.text,104,y+29,215,46,12,Muted);
                }
            }
            else
            {
                int met=game.Residents.FindAll(x=>x.State.met).Count;
                Quest(container,0,"01  初来乍到","和三位邻居分别聊一次天",met,3,"完成后：全员好感 +3");
                Quest(container,183,"02  小镇漫步","走到四座建筑的门前",game.Save.visited.Count,4,"把工坊、工作室、咖啡馆和图书馆串成一条路。");
                Quest(container,366,"03  故事的开始","与一位居民成为朋友",game.Residents.Exists(x=>x.State.affinity>=40)?1:0,1,"多聊聊共同的兴趣，友谊会随记忆一起生长。");
            }
        }
        void Quest(Transform parent,float y,string title,string desc,int value,int total,string reward)
        {
            bool done=value>=total;var box=Panel(parent,"Wish",new Rect(22,y,300,165),H("#e8eadf"));
            Label(box,title+(done?"  ✓":""),17,14,268,27,18,Ink,FontStyle.Bold);
            Label(box,desc,17,53,268,23,13,Muted);
            Panel(box,"Progress",new Rect(17,90,264,5),H("#d1d9cc"),false);
            Panel(box,"Filled progress",new Rect(17,90,264*Mathf.Clamp01(value/(float)total),5),Green,false);
            Label(box,value+" / "+total,17,109,60,22,13,Green);
            Label(box,reward,84,108,198,43,11,Muted);
        }
        void BuildWorldLabels()
        {
            var labels=new GameObject("World captions",typeof(RectTransform));labels.transform.SetParent(root,false);
            var rt=labels.GetComponent<RectTransform>();rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;
            foreach(var r in game.Residents)
            {
                Text name=Label(rt,r.Name,0,0,130,27,14,Ink,FontStyle.Bold);name.alignment=TextAnchor.MiddleCenter;
                var shadow=name.gameObject.AddComponent<Outline>();shadow.effectColor=new Color(1,1,.9f,.7f);shadow.effectDistance=new Vector2(1,-1);worldNames.Add(name);
                Text bubble=Label(rt,"",0,0,240,54,12,Ink);bubble.alignment=TextAnchor.MiddleCenter;
                var bg=bubble.gameObject.AddComponent<Outline>();bg.effectColor=new Color(1,1,.9f,.85f);bg.effectDistance=new Vector2(1,-1);bubbles.Add(bubble);
            }
            foreach(var entry in game.World.Signs)
            {
                var sign=Label(rt,entry.Value,0,0,160,23,11,H("#4a6153"),FontStyle.Bold);sign.alignment=TextAnchor.MiddleCenter;signs.Add(sign);
            }
            playerLabel=Label(rt,"旅人 · 你",0,0,130,24,12,Ink);playerLabel.alignment=TextAnchor.MiddleCenter;
            waypoint=Label(rt,"◇",0,0,40,40,30,Green);waypoint.alignment=TextAnchor.MiddleCenter;
        }
        void BuildChat()
        {
            chat=Panel(root,"Conversation",new Rect(80,490,880,426),Cream);chatObject=chat.gameObject;
            Panel(chat,"Accent",new Rect(0,0,6,426),Green,false);
            chatTitle=Label(chat,"",26,18,570,30,23,Ink,FontStyle.Bold);
            chatSource=Label(chat,"",26,54,640,22,12,Muted);
            Button(chat,"记忆档案",680,20,114,36,()=>ShowProfile(game.Talking),H("#e2e7db"),Ink,13);
            Button(chat,"×",812,17,42,40,()=>{if(!game.Busy)game.CloseChat();else game.Notify("正在等待回复，请稍候。");},H("#e8e8dd"),Ink,22);
            chatScroll=CreateScroll(chat,new Rect(26,91,822,184),out transcript);
            string[] quick={"你好，介绍一下自己吧","最近在忙什么？","你记得我们聊过什么吗？"};
            for(int i=0;i<3;i++){string text=quick[i];Button(chat,new[]{"认识一下","聊聊近况","回忆往事"}[i],26+i*151,288,139,31,()=>{if(!game.Busy)game.Send(TownLanguage.Text(text));},H("#e5e8dd"),Green,12);}
            input=Field(chat,26,332,702,46,"写下你想说的话…",false);
            input.characterLimit=1000;input.onEndEdit.AddListener(value=>{if(UnityEngine.InputSystem.Keyboard.current!=null&&UnityEngine.InputSystem.Keyboard.current.enterKey.isPressed)Submit();});
            sendLabel=Button(chat,"发送  ↵",742,332,109,46,Submit,Green,Cream,15).GetComponentInChildren<Text>();
            status=Label(chat,"每次对话都会留下记忆。",26,391,800,20,12,Muted);
            chatObject.SetActive(false);
        }
        void Submit(){if(game.Busy)return;string text=input.text;if(string.IsNullOrWhiteSpace(text))return;input.text="";game.Send(text);}
        public void ShowChat(Resident r)
        {
            chatObject.SetActive(true);chatTitle.text=r.Name+"  /  "+r.Role;
            chatSource.text=r.Location+"   ·   "+Resident.Relationship(r.State.affinity)+"  "+r.State.affinity.ToString("0")+" / 100   ·   "+(game.Preferences.useBackend?game.Connection:"本地演示");
            transcript.text="";
            foreach(var m in r.State.memories.GetRange(Mathf.Max(0,r.State.memories.Count-12),Mathf.Min(12,r.State.memories.Count)))
                AppendTranscript($"<color=#8b9689>{(TownLanguage.English?"You":"你")}  ·  {Escape(TownLanguage.Text(m.time))}</color>\n{Escape(m.player)}\n\n<color=#688e77>{TownLanguage.Text(r.Name)}</color>\n{Escape(m.reply)}\n\n");
            if(r.State.memories.Count==0)transcript.text=$"<color=#688e77>{r.Name}</color>\n"+(r.Id=="zhang"?"嗨！刚好忙完一段代码。有什么想聊的吗？":r.Id=="li"?"欢迎来小镇！我正想认识一些新朋友。":"你好呀。今天的阳光很适合散步，要坐下来聊聊吗？")+"\n\n";
            if(r.State.memories.Count==0)transcript.text=TownLanguage.Text(transcript.text);
            input.text="";status.text="自由输入，或选择一个话题开始。";sendLabel.text="发送  ↵";ScrollBottom();input.ActivateInputField();
        }
        public void HideChat(){chatObject.SetActive(false);}
        public void BeginMessage(string message)
        {AppendTranscript("<color=#8b9689>"+(TownLanguage.English?"You":"你")+"  ·  "+game.Clock+"</color>\n"+Escape(message)+"\n\n");status.text=game.Preferences.useBackend?"正在回忆并组织回答…":"正在翻开记忆的小本子…";sendLabel.text="等待中…";input.interactable=false;ScrollBottom();}
        public void EndMessage(Resident r,string reply,string source,float delta)
        {
            AppendTranscript("<color=#688e77>"+TownLanguage.Text(r.Name)+"</color>\n"+Escape(reply)+"\n\n");
            status.text=source+"   ·   记忆已保存   ·   好感 "+(delta>=0?"+":"")+delta.ToString("0")+"   ·   "+Resident.Relationship(r.State.affinity);
            chatSource.text=r.Location+"   ·   "+Resident.Relationship(r.State.affinity)+"  "+r.State.affinity.ToString("0")+" / 100";
            sendLabel.text="发送  ↵";input.interactable=true;ScrollBottom();if(chatObject.activeSelf)input.ActivateInputField();
        }
        void ScrollBottom(){Canvas.ForceUpdateCanvases();chatScroll.verticalNormalizedPosition=0;}
        void NewModal(string title,int width,int height)
        {
            if(modalObject)Destroy(modalObject);
            veil=Panel(root,"Modal veil",new Rect(0,0,Width,1000),new Color(.1f,.18f,.14f,.5f));modalObject=veil.gameObject;
            modal=Panel(veil,title,new Rect((Width-width)/2f,(1000-height)/2f,width,height),Cream);
            Label(modal,title,28,24,width-100,36,25,Ink,FontStyle.Bold);
            Button(modal,"×",width-65,22,36,36,()=>modalObject.SetActive(false),H("#e2e6da"),Ink,22);
        }
        public void ShowProfile(Resident r)
        {
            if(r==null)return;
            NewModal("居民档案",650,718);
            Portrait(modal,r,28,85,82);
            Label(modal,r.Name+"  /  "+r.Role,136,89,472,30,24,Ink,FontStyle.Bold);
            Label(modal,r.Personality,136,133,472,44,14,Muted);
            Label(modal,"擅长  "+r.Expertise,28,192,590,26,14,Ink);
            Label(modal,$"关系  {Resident.Relationship(r.State.affinity)}  {r.State.affinity:0}/100     心情  {r.State.mood:0}     精力  {r.State.energy:0}",28,229,590,25,14,Green);
            Label(modal,"共同的记忆   /   "+r.State.memories.Count+" 条"+(r.State.memories.Count>20?" · 最近 20 条":""),28,280,590,27,17,Ink,FontStyle.Bold);
            CreateScroll(modal,new Rect(28,324,590,294),out Text memories);
            if(r.State.memories.Count==0)memories.text=TownLanguage.Text("你们的故事还是一张空白页。\n去和 TA 打个招呼，第一段记忆就会从这里开始。");
            else for(int i=r.State.memories.Count-1;i>=Mathf.Max(0,r.State.memories.Count-20);i--)
            {var m=r.State.memories[i];memories.text+=$"<color=#8b9689>{Escape(TownLanguage.Text(m.time))}  ·  {Escape(TownLanguage.Text(m.source))}</color>\n{(TownLanguage.English?"You":"你")}: {Escape(Preview(m.player,180))}\n{TownLanguage.Text(r.Name)}: {Escape(Preview(m.reply,350))}\n\n";}
            Label(modal,"最近 10 条构成短期上下文；长期记忆会用于回忆相关话题。",28,646,590,43,12,Muted);
        }
        public void ShowSettings()
        {
            NewModal("小镇设置",650,740);
            Button(modal,game.Preferences.english?"Language: English → 中文":"语言：中文 → English",28,610,279,43,()=>{if(!game.Busy)game.ChangeLanguage();},H("#e3e7da"),Ink,14);
            Button(modal,"重新开始",325,610,296,43,()=>
            {
                if(game.Busy)return;
                NewModal("重新开始",650,300);
                Label(modal,"旧进度会先备份；新游戏使用独立的 AI 记忆。",28,95,590,70,16,Ink);
                Button(modal,"确认开始新游戏",28,200,590,46,()=>game.NewGame(),Green,Cream,15);
            },H("#e3e7da"),Ink,14);
            Label(modal,"对话方式",28,92,590,26,17,Ink,FontStyle.Bold);
            Button(modal,"本地演示",28,137,279,46,()=>{game.Preferences.useBackend=false;game.Connection="本地演示";game.SaveGame(false);game.Notify("已切换本地演示 · 使用内置规则与本地记忆");},H("#e1e7d8"),Ink,15);
            Button(modal,"AI 后端",325,137,296,46,()=>{if(!ApplyEndpoint())return;game.Preferences.useBackend=true;game.SaveGame(false);StartCoroutine(game.CheckBackend());},Green,Cream,15);
            Label(modal,"本地演示无需联网。AI 模式需启动项目中的 Python 后端，\n并在 Backend/.env 填写你自己的模型配置。",28,204,590,58,14,Muted);
            Label(modal,"后端地址",28,284,590,26,16,Ink,FontStyle.Bold);
            endpoint=Field(modal,28,326,440,46,"http://127.0.0.1:8000",false);endpoint.text=game.Preferences.endpoint;
            Button(modal,"测试连接",484,326,137,46,()=>{if(ApplyEndpoint())StartCoroutine(game.CheckBackend());},H("#dfe6d7"),Ink,14);
            Label(modal,"模型密钥仅放在后端环境文件中。界面中会明确显示\n「本地演示」「后端演示」或「AI 已连接」。",28,398,590,58,13,Muted);
            Button(modal,game.Preferences.sound?"音乐：开（点击切换）":"音乐：关（点击切换）",28,489,279,43,()=>{game.SetSound(!game.Preferences.sound);game.SaveGame(false);ShowSettings();},H("#e3e7da"),Ink,14);
            Button(modal,"保存设置",325,489,296,43,()=>{if(ApplyEndpoint()){game.SaveGame(true);modalObject.SetActive(false);}},Green,Cream,14);
            Label(modal,"存档位置："+TownStorage.DirectoryPath,28,557,590,43,10,Muted);
        }
        bool ApplyEndpoint()
        {
            string value=endpoint?endpoint.text.Trim():game.Preferences.endpoint;
            if(!Uri.TryCreate(value,UriKind.Absolute,out var uri)||(uri.Scheme!="http"&&uri.Scheme!="https")||!string.IsNullOrEmpty(uri.UserInfo)) {game.Notify("请输入有效的 HTTP 或 HTTPS 后端地址。");return false;}
            game.Preferences.endpoint=value.TrimEnd('/');return true;
        }
        public void ShowHelp()
        {
            NewModal("从一次相遇开始",650,620);
            string text="01  在小镇里走一走\nWASD / 方向键移动，点击地面自动寻路。滚轮或左侧按钮缩放。放大后镜头会跟随你。\n\n02  认识你的邻居\n靠近居民按 E，或点击角色、右侧「前往交谈」。每个人都有自己的职业、喜好和每日安排。\n\n03  把故事留在记忆里\n对话会改变好感度。告诉居民你的名字或喜好，再问「你还记得吗」。档案里可以查看共同记忆。\n\n04  按自己的节奏生活\n空格暂停居民与时钟，1× / 3× / 6× 调整时间。右侧的动态记录生活，心愿带你探索小镇。\n\n05  接入真正的 AI\n运行根目录的「启动AI后端.bat」，在 Backend/.env 配置模型，然后从设置切换「AI 后端」。本地演示为规则对话。";
            if(TownLanguage.English)text="01  Explore the town\nMove with WASD or arrow keys. Click the ground to walk there. Scroll to zoom.\n\n02  Meet your neighbors\nApproach a resident and press E, or select Go talk. Each resident has a job, interests and a daily routine.\n\n03  Build shared memories\nTell residents about yourself and ask what they remember. View your conversations in their profiles.\n\n04  Set your own pace\nSpace pauses the clock and residents. Use 1x / 3x / 6x to change time speed. Explore the Events and Wishes tabs.\n\n05  Connect AI\nStart the Python backend and choose AI backend in Settings. Your model key belongs in Backend/.env. Local demo uses rules.\n\n06  Start again\nSettings > New game archives your current save and starts with fresh memories.";
            Label(modal,text,28,92,590,454,15,Ink);
            Button(modal,"开始散步  →",28,548,590,43,()=>modalObject.SetActive(false),Green,Cream,15);
        }
        public void CloseTop()
        {if(modalObject&&modalObject.activeSelf)modalObject.SetActive(false);else if(chatObject.activeSelf){if(!game.Busy)game.CloseChat();else game.Notify("正在等待回复，请稍候。");}else ShowHelp();}
        public void SetWaypoint(Vector2 position){waypointPosition=position;waypointUntil=Time.unscaledTime+1.7f;}
        public void Refresh()
        {
            clock.text="第 "+game.Save.day+" 天 · "+game.Clock;
            phase.text=(game.Paused?"生活已暂停":game.Phase+"  ·  晴  ·  "+game.Speed.ToString("0")+"×")+"   22°C";
            pause.text=game.Paused?"▶":"Ⅱ";connection.text="●  "+(game.Preferences.useBackend?game.Connection:"本地演示");
            if(tab==0)
            {
                for(int i=0;i<cardText.Count;i++)
                {
                    var r=game.Residents[i];cardText[i].text=r.Location+"  ·  "+Resident.Relationship(r.State.affinity);
                    cardBars[i].rectTransform.sizeDelta=new Vector2(Mathf.Max(4,198*r.State.affinity/100),4);
                }
                var s=game.Selected;if(detail)detail.text=s.Name+" · "+s.Activity+"\n\n「"+(s.Thought.Length>43?s.Thought.Substring(0,43)+"…":s.Thought)+"」";
            }
            sideRefresh-=Time.unscaledDeltaTime;if(sideRefresh<0){sideRefresh=2;if(tab!=0)RenderSide();}
            if(chatObject.activeSelf){chat.anchoredPosition=new Vector2(Mathf.Max(16,(WorldWidth-880)/2),-490);chat.localScale=Vector3.one*Mathf.Min(1,(WorldWidth-32)/880);}
            for(int i=0;i<game.Residents.Count;i++)
            {
                var r=game.Residents[i];float lift=0;
                for(int j=0;j<i;j++)if(Vector2.Distance(r.Actor.transform.position,game.Residents[j].Actor.transform.position)<1.6f)lift=.48f;
                PositionLabel(worldNames[i],(Vector2)r.Actor.transform.position+Vector2.up*(1.62f+lift),130,27);
                bubbles[i].gameObject.SetActive(r.BubbleTimer>0&&!ModalOpen&&game.Zoom>1.08f);
                if(bubbles[i].gameObject.activeSelf){bubbles[i].text=r.Bubble;PositionLabel(bubbles[i],(Vector2)r.Actor.transform.position+Vector2.up*2.7f,240,54);}
            }
            for(int i=0;i<signs.Count;i++)PositionLabel(signs[i],game.World.Signs[i].Key,160,23);
            PositionLabel(playerLabel,(Vector2)game.Player.transform.position+Vector2.up*1.62f,130,24);
            waypoint.gameObject.SetActive(Time.unscaledTime<waypointUntil&&!ModalOpen);if(waypoint.gameObject.activeSelf)PositionLabel(waypoint,waypointPosition,40,40);
            toastObject.SetActive(Time.unscaledTime<game.ToastUntil);toast.text=game.Toast;
            var near=game.NearestResident();taskHint.text=ModalOpen?"":near!=null?"E  与 "+near.Name+" 交谈":"初来乍到   ·   认识三位邻居   "+game.Residents.FindAll(x=>x.State.met).Count+" / 3";
        }
        void PositionLabel(Text text,Vector2 world,float width,float height)
        {
            Vector3 screen=game.Camera.WorldToScreenPoint(world);bool visible=game.Camera.pixelRect.Contains(screen);
            if(!visible){text.enabled=false;return;}text.enabled=true;
            text.rectTransform.anchoredPosition=new Vector2(screen.x/(Screen.height/1000f)-width/2,-(1000-screen.y/(Screen.height/1000f))+height/2);
        }
        void Portrait(Transform parent,Resident r,float x,float y,float size)
        {
            var circle=Panel(parent,"Portrait",new Rect(x,y,size,size),new Color(r.Color.r,r.Color.g,r.Color.b,.23f),false);
            var image=new GameObject("Avatar",typeof(RectTransform),typeof(Image));image.transform.SetParent(circle,false);
            SetRect(image.GetComponent<RectTransform>(),new Rect(size*.15f,-size*.08f,size*.7f,size*1.05f));
            var t=Resources.Load<Texture2D>("Town/"+r.Sprite);image.GetComponent<Image>().sprite=Sprite.Create(t,new Rect(0,t.height-48,32,48),new Vector2(.5f,.5f),32);image.GetComponent<Image>().preserveAspect=true;image.GetComponent<Image>().raycastTarget=false;
        }
        RectTransform Panel(Transform parent,string name,Rect rect,Color color,bool hit=true)
        {
            var obj=new GameObject(name,typeof(RectTransform),typeof(Image));obj.transform.SetParent(parent,false);
            var rt=obj.GetComponent<RectTransform>();SetRect(rt,rect);var image=obj.GetComponent<Image>();image.color=color;image.raycastTarget=hit;
            if(round&&rect.height>5){image.sprite=round;image.type=Image.Type.Sliced;}
            return rt;
        }
        Text Label(Transform parent,string content,float x,float y,float width,float height,int size,Color color,FontStyle style=FontStyle.Normal,bool raw=false)
        {
            var obj=new GameObject("Text",typeof(RectTransform),raw?typeof(Text):typeof(TownLocalizedText));obj.transform.SetParent(parent,false);
            SetRect(obj.GetComponent<RectTransform>(),new Rect(x,y,width,height));var text=obj.GetComponent<Text>();
            text.font=font;text.fontSize=size;text.fontStyle=style;text.color=color;text.text=content;text.raycastTarget=false;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;text.supportRichText=false;return text;
        }
        Button Button(Transform parent,string title,float x,float y,float width,float height,Action click,Color color,Color textColor,int size)
        {
            var rt=Panel(parent,"Button · "+title,new Rect(x,y,width,height),color);
            var button=rt.gameObject.AddComponent<Button>();button.targetGraphic=rt.GetComponent<Image>();
            var colors=button.colors;colors.normalColor=Color.white;colors.highlightedColor=new Color(.93f,.96f,.91f);colors.pressedColor=new Color(.80f,.86f,.77f);colors.selectedColor=Color.white;button.colors=colors;
            button.navigation=new Navigation{mode=Navigation.Mode.None};button.onClick.AddListener(()=>click());
            var text=Label(rt,title,4,0,width-8,height,size,textColor);text.alignment=TextAnchor.MiddleCenter;return button;
        }
        InputField Field(Transform parent,float x,float y,float width,float height,string placeholder,bool password)
        {
            var rt=Panel(parent,"Text input",new Rect(x,y,width,height),H("#ffffff"));
            var f=rt.gameObject.AddComponent<InputField>();f.targetGraphic=rt.GetComponent<Image>();
            Text text=Label(rt,"",13,9,width-26,height-14,16,Ink,raw:true);text.supportRichText=false;text.verticalOverflow=VerticalWrapMode.Overflow;
            Text ph=Label(rt,placeholder,13,9,width-26,height-14,15,Muted);
            f.textComponent=text;f.placeholder=ph;f.lineType=InputField.LineType.SingleLine;f.contentType=password?InputField.ContentType.Password:InputField.ContentType.Standard;
            f.caretColor=Green;f.selectionColor=new Color(.5f,.65f,.5f,.25f);return f;
        }
        ScrollRect CreateScroll(Transform parent,Rect rect,out Text text)
        {
            var panel=Panel(parent,"Scroll",rect,H("#f4f2e8"));var scroll=panel.gameObject.AddComponent<ScrollRect>();
            panel.gameObject.AddComponent<RectMask2D>();
            text=Label(panel,"",0,0,rect.width-14,rect.height,16,Ink,raw:true);text.supportRichText=true;text.verticalOverflow=VerticalWrapMode.Overflow;
            var fit=text.gameObject.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            scroll.content=text.rectTransform;scroll.viewport=panel;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity=28;text.lineSpacing=1.25f;return scroll;
        }
        static void SetRect(RectTransform rt,Rect r){rt.anchorMin=rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,1);rt.anchoredPosition=new Vector2(r.x,-r.y);rt.sizeDelta=new Vector2(r.width,r.height);}
        static void AnchorRight(RectTransform rt,float x,float y){rt.anchorMin=rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(1,1);rt.anchoredPosition=new Vector2(-x,-y);}
        static void StretchX(RectTransform rt,float height,bool top){rt.anchorMin=new Vector2(0,top?1:0);rt.anchorMax=new Vector2(1,top?1:0);rt.pivot=new Vector2(0,top?1:0);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(0,height);}
        static Sprite MakeRounded()
        {
            int n=32,r=8;var t=new Texture2D(n,n);var pixels=new Color[n*n];
            for(int y=0;y<n;y++)for(int x=0;x<n;x++){float dx=Mathf.Max(r-x,x-(n-r-1)),dy=Mathf.Max(r-y,y-(n-r-1));pixels[y*n+x]=(dx<=0||dy<=0||dx*dx+dy*dy<=r*r)?Color.white:Color.clear;}
            t.SetPixels(pixels);t.Apply();return Sprite.Create(t,new Rect(0,0,n,n),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(r,r,r,r));
        }
    }
}
