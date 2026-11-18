using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

namespace AgentTown
{
    public static class TownLanguage
    {
        public static bool English;
        // Presentation only: stable resident IDs, save keys and original dialogue stay unchanged.
        static readonly Dictionary<string,string> Words=new Dictionary<string,string>
        {
            {"赛博小镇  /  每一次相遇，都留下记忆","Every encounter leaves a memory"},
            {"一座会记住你的 AI 小镇","An AI town that remembers you"},
            {"WASD  移动     点击地面  寻路     E  交谈     滚轮  缩放     空格  暂停","WASD Move    Click Walk    E Talk    Scroll Zoom    Space Pause"},
            {"03 位居民   /   正在发生的生活","03 residents / Life unfolds"},
            {"●  每 30 秒自动保存","● Autosaves every 30 seconds"},
            {"慢慢走，故事会自己发生。","Take your time. Stories will find you."},
            {"对话方式","Conversation mode"},{"本地演示","Local demo"},{"后端演示","Backend demo"},{"AI 后端","AI backend"},{"AI 已连接","AI connected"},{"后端不可用","Backend offline"},{"连接中…","Connecting…"},
            {"本地演示无需联网。AI 模式需启动项目中的 Python 后端，\n并在 Backend/.env 填写你自己的模型配置。","Local demo works offline. For AI conversations, start the Python\nbackend with your model configured in Backend/.env."},
            {"模型密钥仅放在后端环境文件中。界面中会明确显示\n「本地演示」「后端演示」或「AI 已连接」。","Your API key stays in the backend. The status shows whether\nyou are using local demo, backend demo or AI."},
            {"音乐：开（点击切换）","Music: On"},{"音乐：关（点击切换）","Music: Off"},
            {"后端地址","Backend address"},{"测试连接","Test connection"},{"保存设置","Save settings"},{"存档位置：","Save folder: "},
            {"重新开始","New game"},{"旧进度会先备份；新游戏使用独立的 AI 记忆。","Your current save will be archived. The new game starts with fresh AI memories."},{"确认开始新游戏","Start new game"},
            {"暮光溪谷","Twilight Valley"},{"小镇设置","Settings"},{"设置","Settings"},{"保存","Save"},{"玩法说明","How to play"},{"声音 开/关","Sound On/Off"},
            {"小镇手记","Town journal"},{"居民档案","Resident profile"},{"记忆档案","Memories"},{"居民","Residents"},{"动态","Events"},{"心愿","Wishes"},
            {"此刻的故事","Their story right now"},{"前往交谈  →","Go talk  →"},{"档案","Profile"},
            {"01  初来乍到","01  New in town"},{"02  小镇漫步","02  A walk around town"},{"03  故事的开始","03  A story begins"},
            {"和三位邻居分别聊一次天","Talk to all three neighbors"},{"完成后：全员好感 +3","Reward: +3 affinity with everyone"},{"走到四座建筑的门前","Visit all four buildings"},{"把工坊、工作室、咖啡馆和图书馆串成一条路。","Discover the workshop, studio, cafe and library."},{"与一位居民成为朋友","Make a friend"},{"多聊聊共同的兴趣，友谊会随记忆一起生长。","Share your interests. Friendship grows with memories."},
            {"旅人 · 你","Traveler · You"},{"认识一下","Say hello"},{"聊聊近况","Catch up"},{"回忆往事","Remember"},{"你好，介绍一下自己吧","Hello! Tell me about yourself."},{"最近在忙什么？","What have you been working on?"},{"你记得我们聊过什么吗？","Do you remember our conversations?"},
            {"写下你想说的话…","Write a message…"},{"发送  ↵","Send  ↵"},{"等待中…","Waiting…"},{"每次对话都会留下记忆。","Every conversation leaves a memory."},{"自由输入，或选择一个话题开始。","Write a message or pick a topic."},{"正在回忆并组织回答…","Recalling memories and thinking of a reply…"},{"正在翻开记忆的小本子…","Looking through our memories…"},{"正在等待回复，请稍候。","Please wait for the reply."},
            {"嗨！刚好忙完一段代码。有什么想聊的吗？","Hi! I just finished some code. What's on your mind?"},{"欢迎来小镇！我正想认识一些新朋友。","Welcome! I was hoping to meet someone new."},{"你好呀。今天的阳光很适合散步，要坐下来聊聊吗？","Hello! It's a lovely day. Shall we sit and chat?"},
            {"最近 10 条构成短期上下文；长期记忆会用于回忆相关话题。","The latest 10 turns provide context; relevant older memories are recalled."},{"你们的故事还是一张空白页。\n去和 TA 打个招呼，第一段记忆就会从这里开始。","Your story is a blank page.\nSay hello to create your first memory."},
            {"共同的记忆","Shared memories"},{"最近 20 条","Latest 20"},{" 条"," entries"},{"擅长","Interests"},{"关系","Relationship"},{"心情","Mood"},{"精力","Energy"},{"记忆已保存","Memory saved"},{"长期记忆","Long-term memory"},{"好感","Affinity"},
            {"张三","Zhang San"},{"李四","Li Si"},{"王五","Wang Wu"},{"Python 工程师","Python engineer"},{"产品经理","Product manager"},{"UI 设计师","UI designer"},
            {"安静专注，喜欢分享技术，偶尔吐槽 bug","Quiet and focused; loves sharing ideas and joking about bugs."},{"热情健谈，关心每个人的想法","Warm and talkative; curious about everyone's ideas."},{"细腻温柔，喜欢观察光影和喝咖啡","Thoughtful and gentle; loves light, color and coffee."},
            {"多智能体系统、Python、开源","Multi-agent systems, Python, open source"},{"产品规划、需求分析、社区协作","Product planning, research, community"},{"界面设计、配色、交互体验","UI design, color, interaction"},
            {"智能体工坊","Agent Workshop"},{"共创工作室","Co-creation Studio"},{"半日咖啡","Half-day Cafe"},{"小镇图书馆","Town Library"},{"喷泉广场","Fountain Square"},{"工坊前的小路","Workshop path"},{"家门口","Home"},
            {"陌生","Stranger"},{"相识","Acquaintance"},{"友好","Friendly"},{"亲密","Close"},{"挚友","Best friend"},
            {"安静休息","Resting"},{"吃早餐，补充灵感","Breakfast and inspiration"},{"午间休息","Lunch break"},{"调试多智能体系统","Debugging agent systems"},{"整理小镇居民的需求","Listening to residents' needs"},{"绘制新的界面草图","Sketching a new interface"},{"翻阅社区活动资料","Researching community events"},{"专注今天的创作","Working on today's project"},{"在喷泉广场散步","Walking around the fountain"},{"收集黄昏的颜色","Collecting sunset colors"},{"和邻居聊聊今天","Catching up with neighbors"},{"享用早晨的咖啡","Enjoying morning coffee"},{"准备今天的工作","Getting ready for work"},
            {"今天也要让代码优雅一点。","A little more elegance in today's code."},{"好的产品，始于一次认真倾听。","Good products begin with listening."},{"这片树影，很适合做新的配色。","These shadows would make a lovely palette."},
            {"这个 bug，终于有思路了。","Finally, an idea for that bug."},{"代码之外，也要看看天空。","Look up from the code. Enjoy the sky."},{"想把小镇的记忆连成一张图。","I'd love to map the town's memories."},{"先写一个小测试，再喝咖啡。","One small test, then coffee."},{"如果这里能办个分享会就好了。","We should hold a community meetup."},{"想听听大家对小镇的期待。","What does everyone hope for this town?"},{"一杯咖啡的时间，聊一个好点子。","One coffee, one good idea."},{"今天又收集到了新想法。","Another day, another new idea."},{"鼠尾草绿，配一点暖杏色。","Sage green with a touch of apricot."},{"午后的光落在屋顶上，真好看。","The afternoon light on those roofs is beautiful."},{"想给咖啡馆画一张新菜单。","I'd love to draw a new cafe menu."},{"设计里也需要一点留白。","Design needs room to breathe."},
            {"想让每个智能体都记住重要的事。","Every agent should remember what matters."},{"一个好点子，往往从倾听开始。","Good ideas begin with listening."},{"今天的树影，很适合画进草图。","Today's tree shadows belong in a sketch."},
            {"，晚点一起喝杯咖啡？",", coffee later?"},{"好呀，也想听听你的新想法。","Sure! I'd love to hear your ideas."},{"交换了今天的灵感，约好晚点再聊。","Shared ideas and made plans to catch up."},{" 的片刻想法","'s thoughts"},{" 与 "," and "},{" 偶遇"," crossed paths"},
            {"欢迎来到 AgentTown","Welcome to AgentTown"},{"这里的每个人，都有自己的故事。先和邻居打个招呼吧。","Everyone has a story. Say hello to your neighbors."},{"新的一天","A new day"},{"昨夜的故事，成为今天的记忆。","Yesterday's stories become today's memories."},
            {"生活已暂停","Paused"},{"上午","Morning"},{"午后","Afternoon"},{"黄昏","Dusk"},{"夜晚","Night"},{"晴","Clear"},{"第 ","Day "},{" 天",""},{"E  与 ","E  Talk to "},{" 交谈",""},{"初来乍到   ·   认识三位邻居","New in town · Meet your neighbors"},
            {"已开启环境音乐","Music on"},{"已关闭环境音乐","Music off"},{"已保存 · ","Saved · "},{"存档写入失败，请检查磁盘空间。","Could not save. Check available disk space."},{"请输入有效的 HTTP 或 HTTPS 后端地址。","Enter a valid HTTP or HTTPS backend address."},{"已切换本地演示 · 使用内置规则与本地记忆","Local demo enabled · Rules and local memories"},
            {"靠近居民后按 E，或从右侧选择「前往交谈」。","Approach a resident and press E, or select Go talk."},{"正在前往 ","Walking to "},{" 身边…","…"},{"每条消息最多 1000 字。","Messages can contain up to 1,000 characters."},{"后端未返回有效内容，已使用本地演示回复。","Invalid backend reply. Using local demo."},{"后端连接失败，已使用本地演示回复。请检查设置。 ","Backend connection failed. Using local demo; check settings."},{" · 请在 Backend/.env 配置模型 Key"," · Configure your API key in Backend/.env"},
            {"你与 ","You and "},{" 的对话"," talked"},{"记住了一段新故事 · 好感 ","New memory · Affinity "},{"发现地点 · ","Discovered · "},{"新的足迹","A new discovery"},{"你来到了","You visited "},{"心愿完成 · 认识了所有邻居！全员好感 +3","Wish complete · Met all neighbors! +3 affinity for everyone"},{"心愿完成 · 收获第一段友谊。","Wish complete · Your first friendship"},{"从一次相遇开始","Start with hello"},{"开始散步  →","Take a walk  →"}
        };
        static readonly KeyValuePair<string,string>[] Ordered=Words.OrderByDescending(x=>x.Key.Length).ToArray();
        public static string Text(string value)
        {
            if(!English||string.IsNullOrEmpty(value))return value;
            foreach(var pair in Ordered)value=value.Replace(pair.Key,pair.Value);
            return value;
        }
    }
    public sealed class TownLocalizedText : Text
    {
        public override string text { get=>base.text; set=>base.text=TownLanguage.Text(value); }
    }
}
