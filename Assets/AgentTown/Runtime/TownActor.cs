using System.Collections.Generic;
using UnityEngine;

namespace AgentTown
{
    public sealed class TownActor : MonoBehaviour
    {
        public List<Vector2> Path = new List<Vector2>();
        public float Speed = 2.8f;
        public bool Moving => Path.Count > 0;
        public bool Held;
        public SpriteRenderer Body;
        [SerializeField] Sprite[] frames;
        [SerializeField] string artName;
        int direction;
        float anim;
        public void Initialize(string art)
        {
            artName=art;
            var texture = Resources.Load<Texture2D>("Town/" + art); texture.filterMode = FilterMode.Point;
            frames = new Sprite[16];
            for (int dir=0;dir<4;dir++) for(int frame=0;frame<4;frame++)
                frames[dir*4+frame] = Sprite.Create(texture,new Rect(frame*32,texture.height-(dir+1)*48,32,48),new Vector2(.5f,0),32);
            if(!transform.Find("Shadow"))
            {
                var shadow = new GameObject("Shadow");shadow.transform.SetParent(transform,false);
                var sr=shadow.AddComponent<SpriteRenderer>();
                var tex=new Texture2D(32,12); var colors=new Color[32*12];
                for(int y=0;y<12;y++)for(int x=0;x<32;x++)colors[y*32+x]=Mathf.Pow((x-15.5f)/15.5f,2)+Mathf.Pow((y-5.5f)/5.5f,2)<1?new Color(.15f,.24f,.21f,.22f):Color.clear;
                tex.SetPixels(colors);tex.Apply();sr.sprite=Sprite.Create(tex,new Rect(0,0,32,12),new Vector2(.5f,.3f),32);sr.sortingOrder=-700;
            }
            Body=GetComponent<SpriteRenderer>();if(!Body)Body=gameObject.AddComponent<SpriteRenderer>();Body.sprite=frames[0];
        }
        void OnEnable()
        {
            if(!string.IsNullOrEmpty(artName)&&(frames==null||frames.Length!=16||!frames[0]))Initialize(artName);
        }
        public void Go(List<Vector2> path) { Path=path; }
        public void Tick(float dt)
        {
            if(Held || Path.Count==0) { Animate(Vector2.zero,dt);return; }
            Vector2 old=transform.position, next=Vector2.MoveTowards(old,Path[0],Speed*dt);
            transform.position=next;
            if(Vector2.Distance(next,Path[0])<.025f)Path.RemoveAt(0);
            Animate(next-old,dt);
        }
        public void Animate(Vector2 movement,float dt)
        {
            if(!Body||frames==null||frames.Length!=16)return;
            if(movement.sqrMagnitude>.000001f)
            {
                direction=Mathf.Abs(movement.x)>Mathf.Abs(movement.y)?(movement.x<0?1:2):(movement.y>0?3:0);
                anim+=dt*7;
            }
            else anim=0;
            Body.sprite=frames[direction*4+Mathf.FloorToInt(anim)%4];Body.sortingOrder=TownWorld.Order(transform.position);
        }
    }
}
