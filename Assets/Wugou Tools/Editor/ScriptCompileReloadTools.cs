using UnityEditor;
using UnityEditor.Compilation;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine;
using System.Reflection;

namespace Wugou.Editor
{
    /// <summary>
    /// ____DESC:   �ֶ�reload domain ���� 
    /// </summary>
    public class ScriptCompileReloadTools
    {
        /* ˵��
         * ���������� https://docs.unity.cn/cn/2021.3/Manual/DomainReloading.html
         * EditorApplication.LockReloadAssemblies()�� EditorApplication.UnlockReloadAssemblies() ��óɶ�
         * �����С��LockReloadAssemblies3�� ����ֻUnlockReloadAssemblies��һ�� ��ô���ǲ������� ����ҲҪ����ֻUnlockReloadAssemblies3��
         */

        const string menuEnableManualReload = ConstDefines.MenuName + "/Script Load/�����ֶ�Reload Domain";
        const string menuDisenableManualReload = ConstDefines.MenuName + "/Script Load/�ر��ֶ�Reload Domain";
        const string menuRealodDomain = ConstDefines.MenuName + "/Script Load/Unlock Reload %t";

        const string kManualReloadDomain = "ManualReloadDomain";
        const string kFirstEnterUnity = "FirstEnterUnity"; //�Ƿ��״ν���unity 
        const string kReloadDomainTimer = "ReloadDomainTimer";//��ʱ


        /**************************************************/
        //����ʱ��
        static Stopwatch compileSW = new Stopwatch();
        //�Ƿ��ֶ�reload
        static bool IsManualReload => PlayerPrefs.GetInt(kManualReloadDomain, -1) == 1;
        //�������� ������֮�����ݻ���false �������false ��ô��Ҫ����
        static bool tempData = false;

        //https://github.com/INeatFreak/unity-background-recompiler ��������� �����ȡ�Ƿ���ס
        static MethodInfo CanReloadAssembliesMethod;
        static bool IsLocked
        {
            get
            {
                if (CanReloadAssembliesMethod == null)
                {
                    // source: https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/EditorApplication.bindings.cs#L154
                    CanReloadAssembliesMethod = typeof(EditorApplication).GetMethod("CanReloadAssemblies", BindingFlags.NonPublic | BindingFlags.Static);
                    if (CanReloadAssembliesMethod == null)
                        Debug.LogError("Can't find CanReloadAssemblies method. It might have been renamed or removed.");
                }
                return !(bool)CanReloadAssembliesMethod.Invoke(null, null);
            }
        }
        /**************************************************/


        [InitializeOnLoadMethod]
        static void InitCompile()
        {
            //**************����Ҫ�������ע��********************************
            CompilationPipeline.compilationStarted -= OnCompilationStarted;
            CompilationPipeline.compilationStarted += OnCompilationStarted;
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            CompilationPipeline.compilationFinished += OnCompilationFinished;
            //**************************************************************

            //�������¼�����
            AssemblyReloadEvents.beforeAssemblyReload -= OnBeforeAssemblyReload;
            AssemblyReloadEvents.beforeAssemblyReload += OnBeforeAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;

            EditorApplication.playModeStateChanged -= EditorApplication_playModeStateChanged;
            EditorApplication.playModeStateChanged += EditorApplication_playModeStateChanged;


            //Bug �״�������ʱ�� ��������������
            //if (PlayerPrefs.HasKey(kManualReloadDomain))
            //{
            //    Menu.SetChecked(menuEnableManualReload, IsManualReload ? true : false);
            //    Menu.SetChecked(menuDisenableManualReload, IsManualReload ? false : true);
            //}
            FirstCheckAsync();
        }

        //�״δ򿪼��
        async static void FirstCheckAsync()
        {
            await System.Threading.Tasks.Task.Delay(100);
            //�ж��Ƿ��״δ�
            //https://docs.unity.cn/cn/2021.3/ScriptReference/SessionState.html
            if (SessionState.GetBool(kFirstEnterUnity, true))
            {
                SessionState.SetBool(kFirstEnterUnity, false);
                Menu.SetChecked(menuEnableManualReload, IsManualReload ? true : false);
                Menu.SetChecked(menuDisenableManualReload, IsManualReload ? false : true);

                if (IsManualReload)
                {
                    UnlockReloadDomain();
                    LockRealodDomain();
                }
                Debug.Log($"<color=lime>��ǰReloadDomain״̬,�Ƿ��ֶ�: {IsManualReload}</color>");
            }
        }


        //����ģʽ�ı�
        private static void EditorApplication_playModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                    break;
                case PlayModeStateChange.ExitingEditMode:
                    if (tempData)
                    {
                        UnlockReloadDomain();
                        EditorUtility.RequestScriptReload();
                    }
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    tempData = true;
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    break;
            }
        }

        //����ʼ����ű�
        private static void OnCompilationStarted(object obj)
        {
            if (IsManualReload)
            {
                compileSW.Start();
                Debug.Log("<color=yellow>Begin Compile</color>");
            }
        }

        //��������
        private static void OnCompilationFinished(object obj)
        {
            if (IsManualReload)
            {
                compileSW.Stop();
                Debug.Log($"<color=yellow>End Compile ��ʱ:{compileSW.ElapsedMilliseconds} ms</color>");
                compileSW.Reset();
            }
        }

        //��ʼreload domain
        private static void OnBeforeAssemblyReload()
        {
            if (IsManualReload)
            {
                Debug.Log("<color=yellow>Begin Reload Domain ......</color>");
                //��¼ʱ��
                SessionState.SetInt(kReloadDomainTimer, (int)(EditorApplication.timeSinceStartup * 1000));
            }

        }
        //����reload domain
        private static void OnAfterAssemblyReload()
        {
            if (IsManualReload)
            {
                var timeMS = (int)(EditorApplication.timeSinceStartup * 1000) - SessionState.GetInt(kReloadDomainTimer, 0);
                Debug.Log($"<color=yellow>End Reload Domain ��ʱ:{timeMS} ms</color>");
                LockRealodDomain();
            }
        }




        static void LockRealodDomain()
        {
            //���û����ס ��ס
            if (!IsLocked)
            {
                EditorApplication.LockReloadAssemblies();
            }
        }

        static void UnlockReloadDomain()
        {
            //�����ס�� ��
            if (IsLocked)
            {
                EditorApplication.UnlockReloadAssemblies();
            }
        }

        [MenuItem(menuEnableManualReload,priority =1000)]
        static void EnableManualReloadDomain()
        {
            Debug.Log("<color=cyan>�����ֶ� Reload Domain</color>");

            Menu.SetChecked(menuEnableManualReload, true);
            Menu.SetChecked(menuDisenableManualReload, false);

            PlayerPrefs.SetInt(kManualReloadDomain, 1);
            //�༭������ projectsetting->editor->enterPlayModeSetting
            EditorSettings.enterPlayModeOptionsEnabled = true;
            EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload;

            LockRealodDomain();
        }

        [MenuItem(menuDisenableManualReload, priority = 1000)]
        static void DisenableManualReloadDomain()
        {
            Debug.Log("<color=cyan>�ر��ֶ� Reload Domain</color>");

            Menu.SetChecked(menuEnableManualReload, false);
            Menu.SetChecked(menuDisenableManualReload, true);

            PlayerPrefs.SetInt(kManualReloadDomain, 0);
            UnlockReloadDomain();
            EditorSettings.enterPlayModeOptionsEnabled = false;
        }
        //�ֶ�ˢ��
        [MenuItem(menuRealodDomain, priority = 1100)]
        static void ManualReload()
        {
            if (IsManualReload)
            {
                UnlockReloadDomain();
                EditorUtility.RequestScriptReload();
            }
        }
    }


}