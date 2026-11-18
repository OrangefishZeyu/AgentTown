// Test the real Input System path without sending OS keyboard events.
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.IO;
var g=UnityEngine.Object.FindFirstObjectByType<AgentTown.TownGame>();
await Wait.Until(()=>g?.UI!=null);
g.IsSmoke=true;g.Paused=true;g.CloseChat();
g.Player.transform.position=AgentTown.TownNavigation.FromPixel(730,640);
var keyboard=InputSystem.AddDevice<Keyboard>("AgentTownQAKeyboard");
bool movement=false,animation=false;
try
{
    var before=g.Player.transform.position;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.D));
    await Task.Delay(100);var frame=g.Player.Body.sprite;
    await Task.Delay(180);
    movement=g.Player.transform.position.x>before.x+.1f;
    animation=g.Player.Body.sprite!=frame;
    InputSystem.QueueStateEvent(keyboard,new KeyboardState());
    await Task.Delay(50);
}
finally {InputSystem.RemoveDevice(keyboard);}
var result=new{keyboardMovement=movement,walkingAnimation=animation};
File.WriteAllText("Builds/QA/input-system.json",Newtonsoft.Json.JsonConvert.SerializeObject(result));
if(!movement||!animation)throw new Exception("Movement input test failed");
return result;
