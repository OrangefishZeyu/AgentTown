using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AgentTown.Editor
{
    [InitializeOnLoad]
    public static class TownProject
    {
        public const string ScenePath="Assets/Scenes/AgentTown.unity";
        static TownProject()
        {
            EditorApplication.update+=ProcessBuildRequest;
            EditorApplication.delayCall+=()=>
            {
                if(!Application.isBatchMode&&!EditorApplication.isPlayingOrWillChangePlaymode&&!File.Exists(ScenePath))
                    CreateScene();
            };
        }
        static void ProcessBuildRequest()
        {
            const string request="Tools/build-request.txt";
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(request))return;
            string command=File.ReadAllText(request).Trim();File.Delete(request);
            try
            {
                EditorSceneManager.SaveOpenScenes();
                Directory.CreateDirectory("Logs");
                if(command=="smoke"||command=="play")
                {
                    OpenScene();
                    if(command=="smoke")File.WriteAllText("Tools/smoke-active.txt","smoke");
                    var window=EditorWindow.GetWindow(typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView"));
                    window.Focus();window.maximized=true;
                    EditorApplication.isPlaying=true;return;
                }
                if(command=="validate"){Validate();return;}
                BuildWindows();
                File.WriteAllText("Logs/AgentTown-build-result.json","{\"success\":true}");
            }
            catch(Exception ex){File.WriteAllText("Logs/AgentTown-build-result.json",JsonUtility.ToJson(new BuildFailure{error=ex.ToString()},true));Debug.LogException(ex);}
        }
        [Serializable] class BuildFailure{public bool success=false;public string error;}
        [MenuItem("AgentTown/1 · 打开小镇场景")]
        public static void OpenScene(){if(!File.Exists(ScenePath))CreateScene();else EditorSceneManager.OpenScene(ScenePath);}
        [MenuItem("AgentTown/2 · 重建预览场景")]
        public static void CreateScene()
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var preview=new GameObject("AgentTown Preview");
            var world=new TownWorld();world.Build();world.Root.SetParent(preview.transform);
            var camera=new GameObject("Preview Camera").AddComponent<Camera>();camera.transform.SetParent(preview.transform);
            camera.transform.position=new Vector3(0,0,-20);camera.orthographic=true;camera.orthographicSize=17.5f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.56f,.68f,.47f);
            var game=new GameObject("AgentTown · Game");game.AddComponent<TownGame>();
            Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            PlayerSettings.companyName="AgentTown";PlayerSettings.productName="AgentTown";
            PlayerSettings.defaultScreenWidth=1600;PlayerSettings.defaultScreenHeight=1000;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
            PlayerSettings.resizableWindow=true;PlayerSettings.runInBackground=true;
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
            AssetDatabase.SaveAssets();Debug.Log("AGENTTOWN_SCENE_READY");
        }
        [MenuItem("AgentTown/3 · 校验核心系统")]
        public static void Validate()
        {
            TownLanguage.English=true;
            if(TownLanguage.Text("小镇设置")!="Settings"||TownLanguage.Text("AI 已连接")!="AI connected")throw new Exception("English UI translation failed");
            TownLanguage.English=false;
            var world=new TownWorld();world.Build();
            Vector2 start=TownNavigation.FromPixel(728,620);
            Vector2[] goals={TownNavigation.FromPixel(306,410),TownNavigation.FromPixel(1006,414),TownNavigation.FromPixel(306,850),TownNavigation.FromPixel(1006,873),TownNavigation.FromPixel(1450,530)};
            foreach(var goal in goals)
            {
                var path=world.Navigation.FindPath(start,goal);if(path.Count==0)throw new Exception("No route to "+goal);
                foreach(var step in path)if(!world.Navigation.Walkable(step))throw new Exception("Path crosses obstacle");
            }
            var resident=new Resident{Name="张三",Id="zhang",Role="Python 工程师",Location="智能体工坊",Activity="写代码",State=new ResidentState()};
            resident.State.memories.Add(new Memory{player="我叫小林，喜欢咖啡。",reply="记住了。"});
            if(!TownGame.LocalReply(resident,"你还记得我吗").Contains("咖啡"))throw new Exception("Memory recall failed");
            if(TownGame.AffinityDelta("谢谢你的帮助")<=0||TownGame.AffinityDelta("笨蛋")>=0)throw new Exception("Affinity failed");
            var data=new TownSave();data.residents.Add(resident.State);string json=JsonUtility.ToJson(data);
            var roundtrip=JsonUtility.FromJson<TownSave>(json);if(roundtrip.residents[0].memories[0].player!="我叫小林，喜欢咖啡。")throw new Exception("Save roundtrip failed");
            UnityEngine.Object.DestroyImmediate(world.Root.gameObject);
            Directory.CreateDirectory("Builds/QA");File.WriteAllText("Builds/QA/editor-validation.json","{\"paths_to_all_four_buildings\":true,\"bridge_path\":true,\"obstacles\":true,\"local_memory\":true,\"affinity\":true,\"save_roundtrip\":true}");
            Debug.Log("AGENTTOWN_VALIDATION_PASS");
        }
        [MenuItem("AgentTown/4 · 构建 Windows 成品")]
        public static void BuildWindows()
        {
            CreateScene();Validate();
            Directory.CreateDirectory("Builds/AgentTown");
            Directory.CreateDirectory("Logs");
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath},locationPathName="Builds/AgentTown/AgentTown.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.None});
            var messages=new System.Text.StringBuilder();
            foreach(var step in report.steps)foreach(var message in step.messages)messages.AppendLine(message.type+": "+message.content);
            File.WriteAllText("Logs/AgentTown-build-details.log",messages.ToString());
            if(report.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Build failed: "+report.summary.result);
            Debug.Log("AGENTTOWN_BUILD_SUCCESS · "+report.summary.totalSize+" bytes");
        }
        [MenuItem("AgentTown/5 · 运行流程烟雾测试")]
        public static void RunSmoke()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            OpenScene();Directory.CreateDirectory("Tools");File.WriteAllText("Tools/smoke-active.txt","smoke");EditorApplication.isPlaying=true;
        }
        [MenuItem("AgentTown/6 · 打开存档目录")]
        public static void OpenSaves(){EditorUtility.RevealInFinder(Application.persistentDataPath);}
    }
    public sealed class TownTextureImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/AgentTown/Resources/Town/",StringComparison.Ordinal))return;
            var importer=(TextureImporter)assetImporter;importer.textureType=TextureImporterType.Default;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
        }
    }
}
