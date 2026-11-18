using System.Collections.Generic;
using UnityEngine;

namespace AgentTown
{
    public sealed class TownWorld
    {
        public TownNavigation Navigation = new TownNavigation();
        public Transform Root;
        public readonly List<KeyValuePair<Vector2, string>> Signs = new List<KeyValuePair<Vector2, string>>();
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
        public static Sprite Art(string name)
        {
            if (cache.TryGetValue(name, out var s) && s) return s;
            Texture2D t = Resources.Load<Texture2D>("Town/" + name);
            if (!t) { Debug.LogError("缺少美术资源 " + name); return null; }
            t.filterMode = FilterMode.Point;
            s = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(.5f, 0), 32);
            cache[name] = s; return s;
        }
        public static SpriteRenderer Place(Transform parent, string art, Vector2 position, int order = 0)
        {
            var go = new GameObject(art); go.transform.SetParent(parent);
            go.transform.position = position;
            var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = Art(art); sr.sortingOrder = order;
            return sr;
        }
        public void Build()
        {
            Root = new GameObject("Town · 原创像素小镇").transform;
            Place(Root, "ground", new Vector2(0, -16), -1000);
            Navigation.BlockPixels(new Rect(0, 0, 1536, 65)); Navigation.BlockPixels(new Rect(0, 946, 1536, 78));
            Navigation.BlockPixels(new Rect(0, 0, 52, 1024)); Navigation.BlockPixels(new Rect(1252, 0, 284, 482));
            Navigation.BlockPixels(new Rect(1252, 580, 284, 444)); Navigation.BlockPixels(new Rect(1490, 482, 46, 98));
            Building("lab", 146, 126, "01  智能体工坊");
            Building("studio", 846, 126, "02  共创工作室");
            Building("cafe", 146, 568, "03  半日咖啡");
            Building("library", 846, 584, "04  小镇图书馆");
            Prop("fountain", 745, 555, new Rect(693, 493, 104, 65));
            foreach (var p in new[] { new Vector2(595, 470), new Vector2(895, 470), new Vector2(594, 630), new Vector2(895, 630), new Vector2(1185, 880), new Vector2(1185, 265) })
                Prop("bench", p.x, p.y, new Rect(p.x-33, p.y-29, 66, 28));
            foreach (var p in new[] { new Vector2(580, 415), new Vector2(905, 415), new Vector2(580, 700), new Vector2(905, 700), new Vector2(105, 540), new Vector2(1210, 540), new Vector2(690, 870), new Vector2(795, 150) })
                Prop("lamp", p.x, p.y, new Rect(p.x-9, p.y-16, 18, 16));
            var random = new System.Random(71);
            // Border trees, with clear doors and continuous roads.
            foreach (int y in new[] { 134, 278, 434, 696, 844, 945 })
                foreach (int x in new[] { 90, 1215 }) Tree(x + random.Next(-8,8), y, y % 3 == 0);
            foreach (int x in new[] { 200, 360, 520, 870, 1050 }) { Tree(x, 116, x%3==0); Tree(x, 947, x%3!=0); }
            foreach (var p in new[] { new Vector2(510, 335), new Vector2(577, 207), new Vector2(570, 843), new Vector2(1120, 933), new Vector2(1110, 96), new Vector2(1355, 417), new Vector2(1450, 411), new Vector2(1380, 696) }) Tree(p.x,p.y,true);
        }
        void Building(string art, float x, float y, string label)
        {
            Vector2 pos = TownNavigation.FromPixel(x + 160, y + 256);
            Place(Root, art, pos, Order(pos));
            Navigation.BlockPixels(new Rect(x+8, y+12, 306, 236));
            Signs.Add(new KeyValuePair<Vector2, string>(TownNavigation.FromPixel(x+160,y+132),label));
        }
        void Prop(string art, float x, float bottom, Rect collision)
        {
            Vector2 pos = TownNavigation.FromPixel(x, bottom);
            Place(Root, art, pos, Order(pos)); Navigation.BlockPixels(collision);
        }
        void Tree(float x, float bottom, bool pink)
        {
            Prop(pink ? "cherry" : "tree",x,bottom,new Rect(x-12,bottom-20,24,25));
        }
        public static int Order(Vector2 pos) => 100 - Mathf.RoundToInt(pos.y * 32);
    }
}
