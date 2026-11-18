// Verify a complete day's schedule, and capture evening lighting.
using System.IO;
var g=UnityEngine.Object.FindFirstObjectByType<AgentTown.TownGame>();
g.IsSmoke=true;g.CloseChat();g.Paused=false;g.Speed=1;
int[] hours={8,10,12,15,18,21,23},slots={1,2,3,4,5,6,0};
bool schedules=true,paths=true;
for(int i=0;i<hours.Length;i++)
{
    g.Save.minute=hours[i]*60;
    foreach(var r in g.Residents)r.DecisionTimer=0;
    await Task.Delay(180);
    foreach(var r in g.Residents)
    {
        schedules&=r.Schedule==slots[i]&&!string.IsNullOrEmpty(r.Activity)&&!string.IsNullOrEmpty(r.Location);
        foreach(var p in r.Actor.Path)paths&=g.World.Navigation.Walkable(p);
    }
}
g.Save.minute=20*60+15;await Task.Delay(2300);g.Paused=true;
ScreenCapture.CaptureScreenshot("Builds/QA/evening.png");await Task.Delay(500);
var result=new{schedules,paths,slots=7};File.WriteAllText("Builds/QA/schedules.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));
g.Save.minute=540;g.Paused=false;
if(!schedules||!paths)throw new Exception("Schedule validation failed");
return result;
