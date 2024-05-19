using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
#if UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif
using Object = UnityEngine.Object;

namespace vietlabs.fr2
{
    // filter, ignore anh huong ket qua thi hien mau do
    // optimize lag duplicate khi use
    public class FR2_Window : EditorWindow, IHasCustomMenu
    {
        internal static FR2_Window window
        {
            get{
                if(_window == null)
                {
                    Initialize();
                }
                return _window;
            }
        }
        private static FR2_Window _window;

        internal class Icon
        {
            private static Icon _icon;
            public static Icon icons
            {
                get{
                    if(_icon == null)
                    {
                        _icon = new Icon();
                        _icon.Refresh = EditorGUIUtility.IconContent("d_Refresh");
                        _icon.LockOn = EditorGUIUtility.IconContent("LockIcon-On");
                        _icon.Lock =EditorGUIUtility.IconContent("LockIcon");
                    }
                    return _icon;
                }
            }
            
            public GUIContent Refresh;
            public GUIContent Lock;
            public GUIContent LockOn;
        }


        [NonSerialized] internal FR2_DuplicateTree2 Duplicated;

        // [NonSerialized] internal FR2_RefInScene RefInScene;
        [NonSerialized] internal FR2_RefDrawer RefInScene;

        // [NonSerialized] internal FR2_RefSceneInScene RefSceneInScene;
        [NonSerialized] internal FR2_RefDrawer RefSceneInScene;

        internal int level;
        private Vector2 scrollPos;

        [NonSerialized] internal List<FR2_Asset> Selected;

        private int selectedTab;
        private string tempGUID;
        private Object tempObject;

        [SerializeField] private bool lockSelection;

        //[NonSerialized] List<FR2_DuplicateInfo> duplicateArray;
        //[NonSerialized] FR2_ScrollList duplicateScroller;

        [NonSerialized] private List<FR2_Asset> unusedArray;
        [NonSerialized] private FR2_TreeUI unusedScroller;
        //[NonSerialized] internal FR2_RefTree<FR2_Asset> UsedBy;
        //[NonSerialized] internal FR2_RefTree<FR2_Asset> Uses;

        [NonSerialized] internal FR2_RefDrawer UsesDrawer;
        [NonSerialized] internal FR2_RefDrawer SceneUsesDrawer; //scene use another scene objects
        [NonSerialized] internal FR2_RefDrawer UsedByDrawer;
        [NonSerialized] internal FR2_RefDrawer SceneToAssetDrawer;
        // private bool foldoutUseInScene = true;
        // private bool foldoutUseInAsset = true;

        private bool IsFocusingUses { get { return selectedTab == 0; } }
        private bool IsFocusingUsedBy { get { return selectedTab == 1; } }
        public bool IsFocusingDuplicate { get { return selectedTab == 2; } }
        private bool IsFocusingUnused { get { return selectedTab == 300; } }
        private bool IsFocusingGUIDs { get { return selectedTab == 3; } }
        private bool IsFocusingFindInScene { get { return selectedTab == 1; } }
        private bool IsFocusingSceneToAsset { get { return selectedTab == 0; } }
        private bool IsFocusingSceneInScene { get { return selectedTab == 1; } }

        public float sizeRatio = 0.5f;
        private Rect resizer;
        private bool isResizing;
        private GUIStyle resizerStyle;

        public bool isNoticeIgnore;

        public void AddItemsToMenu(GenericMenu menu)
        {
            var api = FR2_Cache.Api;
            if (api == null) return;

            menu.AddDisabledItem(new GUIContent("FR2 - v2.0"));
            menu.AddSeparator(string.Empty);

            menu.AddItem(new GUIContent("Enable"), !api.disabled, () =>
            {
                api.disabled = !api.disabled;
            });

            menu.AddItem(new GUIContent("Clear Selection"), false, () =>
            {
                FR2_Selection.h.Clear();
            });

            menu.AddItem(new GUIContent("Commit Selection (" + FR2_Selection.h.Count + ")"), false, () =>
            {
                FR2_Selection.Commit();
            });

            menu.AddItem(new GUIContent("Refresh"), false, () =>
            {
                FR2_Cache.Api.Check4Changes(true, true);
                FR2_SceneCache.Api.SetDirty();
            });

//#if FR2_DEV
		    menu.AddItem(new GUIContent("Refresh Usage"), false, () => FR2_Cache.Api.Check4Usage());
		    menu.AddItem(new GUIContent("Refresh Selected"), false, ()=> FR2_Cache.Api.RefreshSelection());
		    menu.AddItem(new GUIContent("Clear Cache"), false, () => FR2_Cache.Api.Clear());
//#endif

        }

        [MenuItem("Window/Find Reference 2")]
        private static void Initialize()
        {
            if (_window == null)
            {
                _window = GetWindow<FR2_Window>();
                _window.Init();
            }

            FR2_Unity.SetWindowTitle(_window, "FR2");
            _window.Show();
        }
        void OnEnable()
        {
            resizerStyle = new GUIStyle();
            resizerStyle.normal.background = EditorGUIUtility.Load("icons/d_AvatarBlendBackground.png") as Texture2D;
        }
        private void Init()
        {
            //Uses = new FR2_RefTree<FR2_Asset>(FR2_Asset.FindUsage, DrawAsset);
            //UsedBy = new FR2_RefTree<FR2_Asset>(FR2_Asset.FindUsedBy, DrawAsset);

            UsesDrawer = new FR2_RefDrawer()
            {
	            Lable = "Assets"
            };
            SceneUsesDrawer = new FR2_RefDrawer()
            {
                Lable = "Scene Objects",
                isDrawRefreshSceneCache = true
            };

            UsedByDrawer = new FR2_RefDrawer()
            {
	            Lable = "Assets"
            };
            Duplicated = new FR2_DuplicateTree2();
            RefInScene = new FR2_RefDrawer()
            {
                Lable = "Scene Objects",
                isDrawRefreshSceneCache = true
            };

            // RefInScene      = new FR2_RefInScene(this);
            SceneToAssetDrawer = new FR2_RefDrawer()
            {
	            Lable = "Assets",
                isDrawRefreshSceneCache = true
            };
            RefSceneInScene = new FR2_RefDrawer()
            {
	            Lable = "Scene Objects",
                isDrawRefreshSceneCache = true
            };

            //    (item=>item.GetChildren(), (item, rect, s, b) =>
            //{
            //    item.Draw(rect);
            //    return 16f;
            //});

            //Uses.Sorter = FR2_Asset.SortByExtension;
            //UsedBy.Sorter = FR2_Asset.SortByExtension;

            //Uses.BriefDrawer = DrawBrief;
            //UsedBy.BriefDrawer = DrawBrief;

            FR2_Cache.onReady -= OnReady;
            FR2_Cache.onReady += OnReady;

            FR2_Setting.OnIgnoreChange -= OnSelectionChange;
            FR2_Setting.OnIgnoreChange += OnSelectionChange;

            #if UNITY_2018_OR_NEWER
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -= OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode += OnSceneChanged;
            #elif UNITY_2017_OR_NEWER
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += OnSceneChanged;
            #endif
            //Debug.Log(" FR2_Window ---> Init");
        }
#if UNITY_2018_OR_NEWER
        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            if (IsFocusingFindInScene || IsFocusingSceneToAsset || IsFocusingSceneInScene)
            {
                OnSelectionChange();
            }
        }
        #endif

        public Dictionary<string, FR2_Ref> GetUsedByRefs()
        {
            return UsedByDrawer.getRefs();
        }
        private void OnReady()
        {
            OnSelectionChange();

            //if (IsFocusingDuplicate)
            //{
            //    RefreshDuplicate(false);
            //}

            if (IsFocusingUnused)
            {
                unusedArray = FR2_Cache.Api.ScanUnused();
            }
        }

        //private float DrawAsset(FR2_Asset asset, Rect r, bool highlight)
        //{                      

        //    return asset.Draw(r, highlight, true);
        //}

        //private float DrawBrief(FR2_Asset asset, Rect r, bool highlight)
        //{
        //    return asset.Draw(r, false, false);
        //}

        public void OnSelectionChange()
        {
            isNoticeIgnore = false;
            if (!FR2_Cache.isReady) return;
            if (lockSelection) return;
            if (focusedWindow == null) return;
            if (UsesDrawer == null) Init();

            if(IsFocusingSceneToAsset)
                SceneToAssetDrawer.Reset(Selection.gameObjects, true);
            if(IsFocusingSceneInScene)
                RefSceneInScene.ResetSceneInScene(Selection.gameObjects);
            
            if(IsFocusingUses)
            {
                SceneUsesDrawer.ResetSceneUseSceneObjects(Selection.gameObjects);
            }
            if (IsFocusingSceneToAsset || IsFocusingSceneInScene)
            {
                Repaint();
            }
            ids = FR2_Unity.Selection_AssetGUIDs;

            //ignore selection on asset when selected any object in scene
	        if(Selection.gameObjects.Length > 0 && !FR2_Unity.IsInAsset(Selection.gameObjects[0]))
            {
                ids = new string[0];
            }


            //if (focusedWindow.GetType().Name != "ProjectBrowser")
            //{
            //    //Debug.Log(focusedWindow.GetType().Name);
            //    return;
            //}


            //if (Uses == null) Init();

            //Selected = FR2_Cache.Api.FindAssets(FR2_Unity.Selection_AssetGUIDs, true);

            level = 0;
            //Uses.Reset(Selected);
            //UsedBy.Reset(Selected);
            if(IsFocusingUses)
                UsesDrawer.Reset(ids, true);
            if(IsFocusingUsedBy)
                UsedByDrawer.Reset(ids, false);

            if(IsFocusingFindInScene)
                RefInScene.Reset(ids);

            if(IsFocusingGUIDs)
            {
                
                objs = new Object[ids.Length];
                for(int i = 0; i < ids.Length; i++)
                {
                    objs[i] =FR2_Unity.LoadAssetAtPath<Object>
                        (
                            AssetDatabase.GUIDToAssetPath(ids[i])
                        );
                }
            }
            

            Repaint();
            EditorApplication.delayCall += Repaint;
        }

        bool DrawEnable()
        {
            var api = FR2_Cache.Api;
            if (api == null) return false;

            var v = api.disabled;

            if (v)
            {
                EditorGUILayout.HelpBox("Find References 2 is disabled!", MessageType.Warning);
                if (GUILayout.Button("Enable"))
                {
                    api.disabled = !api.disabled;
                    Repaint();
                }

                return !api.disabled;
            }

            if (!api.ready)
            {
                var w = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 70f;
                api.priority = EditorGUILayout.IntSlider("Priority", api.priority, 0, 5);
                EditorGUIUtility.labelWidth = w;
            }

            return !api.disabled;
        }

        private static GUIContent[] TOOLBARS = new GUIContent[]
        {
            new GUIContent("Uses"),
            new GUIContent("Used By"),
            new GUIContent("Duplicate"),
            new GUIContent("GUIDs")
        };
        // public static Rect windowRect;
        internal interface IRefDraw
        {
            int ElementCount();
            bool Draw();
        }
        private class RefDrawConfig
        {
            public IRefDraw refDrawer;
            public bool isDraw;
            public bool isDrawTool;
            public bool drawInTop;
            public bool drawGlobal;
            public RefDrawConfig(IRefDraw drawer)
            {
                refDrawer = drawer;
            }
        }
        private bool GetDrawConfig(IRefDraw refDrawer, out RefDrawConfig config)
        {
            bool drawTool = AnyToolInBot;
            config = new RefDrawConfig(refDrawer);
            config.drawGlobal = !drawTool;
            config.drawInTop = drawTool;
            config.isDraw = true;
            return drawTool;
        }
        //return true if draw tool along
        private bool GetDrawConfig(IRefDraw refTop,IRefDraw  refBot, out RefDrawConfig config1, out RefDrawConfig config2)
        {
            bool drawTool = true;
            config1 = new RefDrawConfig(refTop);
            config2 = new RefDrawConfig(refBot);
	        config1.isDraw = config1.refDrawer.ElementCount() > 0;
	         config2.isDraw = config2.refDrawer.ElementCount() > 0;
	        //config1.isDraw =true;
	        //config2.isDraw =true;
            if(AnyToolInBot)
            {
                if(refTop.ElementCount() > 0)
                {
                    config1.drawInTop = true;
                    if (refBot.ElementCount() > 0)
                    {
                        drawTool = false;
                        config2.isDrawTool = true;
                    }
                }
                else
                {
                    config2.drawInTop = true;
                }
            }
            else
            {
                drawTool = false;
                if(refTop.ElementCount() > 0)
                {
                    config1.drawInTop = true;
                    if (refBot.ElementCount() > 0)
                    {
                        config2.drawInTop = false;
                    }
                    else
                    {
                        config1.drawGlobal = true;
                    }
                }
                else
                {
                    // config2.drawInTop = true;
                    config2.drawGlobal = true;
                }
            }
            return drawTool;
        }
        private int DrawConfig(RefDrawConfig config, Rect rectTop, Rect rectBot, ref bool WillRepaint)
        {
            if(config == null) return 0;
            if(config.refDrawer == null) return 0;
            if(!config.isDraw) return 0;
            bool willRepaint = false;
            if(config.drawGlobal)
            {
                willRepaint = config.refDrawer.Draw();
                
            }
            else
            {
                if(config.drawInTop)
                {
                    GUILayout.BeginArea(rectTop);
                   willRepaint =  config.refDrawer.Draw();
                    GUILayout.EndArea();
                }
                else if(config.isDrawTool)
                {
                    GUILayout.BeginArea(rectBot);
                    willRepaint = config.refDrawer.Draw();
                    DrawTool();
                    GUILayout.EndArea();
                }
                else
                {
                    GUILayout.BeginArea(rectBot);
                    willRepaint = config.refDrawer.Draw();
                    GUILayout.EndArea();
                }
            }
            if(willRepaint) WillRepaint = true;
            return 1;

        }
        private void OnGUI()
        {
            if (window == null)
            {
                Initialize();
            }

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorGUILayout.HelpBox("FR2 requires serialization mode set to FORCE TEXT!", MessageType.Warning);
                if (GUILayout.Button("FORCE TEXT"))
                {
                    EditorSettings.serializationMode = SerializationMode.ForceText;
                }
                return;
            }
            if (FR2_Cache.hasCache && !FR2_Cache.CheckSameVersion())
                {
                    EditorGUILayout.HelpBox("Incompatible cache version found, need a full refresh may take time!", UnityEditor.MessageType.Warning);
                    if (GUILayout.Button("Scan project"))
                    {
                        FR2_Cache.DeleteCache();
                        FR2_Cache.CreateCache();
                    }
                    return;
                }
            if (!FR2_Cache.isReady)
            {
                if (!FR2_Cache.hasCache)
                {
                    EditorGUILayout.HelpBox("FR2 cache not found!\nFirst scan may takes quite some time to finish but you would be able to work normally while the scan works in background...", MessageType.Warning);
                    if (GUILayout.Button("Scan project"))
                    {
                        FR2_Cache.CreateCache();
                    }
                    return;
                }

                if (!DrawEnable()) return;

                var api = FR2_Cache.Api;
                var text = "Refreshing ... " + (int)(api.progress * api.workCount) + " / " + api.workCount;
                var rect = GUILayoutUtility.GetRect(1f, Screen.width, 18f, 18f);
                EditorGUI.ProgressBar(rect, api.progress, text);
                Repaint();
                return;
            }


            if (!DrawEnable()) return;

            var willRepaint = Event.current.type == UnityEngine.EventType.ScrollWheel;
            var newTab = selectedTab;

            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                var v = GUILayout.Toggle(lockSelection,"L", EditorStyles.toolbarButton, GUILayout.Width(16f));
                
                if (v != lockSelection)
                {
                    lockSelection = v;
                    if (lockSelection == false)
                    {
                        OnSelectionChange();
                    }
                    willRepaint = true;
                }
                // var style = EditorStyles.toolbarButton;
                // style.fontSize *= 2;
                // if(GUILayout.Button(lockSelection ? Icon.icons.Lock : Icon.icons.LockOn, style, GUILayout.Width(16f)))
                // {
                //     lockSelection = !lockSelection;
                //     if (lockSelection == false)
                //     {
                //         OnSelectionChange();
                //     }
                //     willRepaint = true;
                // }
                for(int i =0; i< TOOLBARS.Length; i++)
                {
                    bool isSelected = selectedTab == i;
                   var b = GUILayout.Toggle(isSelected, TOOLBARS[i], EditorStyles.toolbarButton);
                    if(b != isSelected)
                    {
                        newTab = i;
                    }
                }
                // newTab = GUILayout.Toolbar(selectedTab, TOOLBARS);
            }
            GUILayout.EndHorizontal();

            if (newTab != selectedTab)
            {
                selectedTab = newTab;
                // Check4Changes means delay calls to OnReady :: Refresh !
                //if (FR2_Cache.Api.isReady) FR2_Cache.Api.Check4Changes();
                OnReady();
            }

            if (Selected == null)
            {
                Selected = new List<FR2_Asset>();
            }

            //if (Selected.Count == 0){
            //	GUILayout.Label("Nothing selected");
            //} 
            Rect rectTop = FR2_Window.window == null ? new Rect() : FR2_Window.window.GetTopPanelRect();
            Rect rectBot = FR2_Window.window == null ? new Rect() : FR2_Window.window.GetBotPanelRect();
            
            RefDrawConfig config1 = null, config2 = null, config3 = null;
             bool drawTool = false;
             int drawCount = 0;
            if(IsFocusingUses || IsFocusingSceneToAsset)
            {
                if(UsesDrawer.ElementCount() <= 0)
                {
                   drawTool = GetDrawConfig(SceneUsesDrawer, SceneToAssetDrawer, out config1, out config2); 
                }
                else
                {
                    drawTool = GetDrawConfig(UsesDrawer, out config3);
                }
                // drawTool = GetDrawConfig(UsesDrawer, SceneToAssetDrawer, out config1, out config2);
            }
            else if(IsFocusingSceneInScene || IsFocusingFindInScene || IsFocusingUsedBy)
            {
                if(RefSceneInScene.ElementCount() <= 0)
                {

	                drawTool = GetDrawConfig(RefInScene, UsedByDrawer, out config1, out config2);
                }
                else
                {
                    drawTool = GetDrawConfig(RefSceneInScene, out config3);

                }
            }
            else if(IsFocusingDuplicate)
            {
                drawTool = GetDrawConfig(Duplicated, out config1);
            }
            else if(IsFocusingGUIDs)
            {
                drawCount++;
                if(AnyToolInBot)
                {
                    GUILayout.BeginArea(rectTop);
                    DrawGUIDs();
                    GUILayout.EndArea();
                    drawTool = true;
                }
                else
                {
                    DrawGUIDs();
                    drawTool = false;
                }
            }
            
            if(!IsFocusingGUIDs)
            {
                drawCount += DrawConfig(config1, rectTop, rectBot, ref willRepaint);
                drawCount += DrawConfig(config2, rectTop, rectBot, ref willRepaint);
                drawCount += DrawConfig(config3, rectTop, rectBot, ref willRepaint);
            }
            if(drawTool)
            {
                drawCount++; 
                GUILayout.BeginArea(rectBot);
                   DrawTool();
                    GUILayout.EndArea();
                
            }


            DrawFooter();
            

            if(willRepaint || this.WillRepaint || !FR2_SceneCache.ready)
            {
                this.WillRepaint = false;
                Repaint();
            }
            if(drawCount > 1)
            {
                DrawResizer();
                ProcessEvents(Event.current);
            }

            if (GUI.changed)
                Repaint();
        }
        private void DrawTool()
        {
             if (showFilter)
            {
                if (AssetType.DrawSearchFilter())
                {
                    maskDirty();
                }
            }
            else if (showIgnore)
            {
                GUILayout.BeginHorizontal(EditorStyles.toolbar);
                GUILayout.Label("Ignores");
						// showContent = EditorGUILayout.Foldout(showContent, searchLable);
					GUILayout.EndHorizontal();
                if (AssetType.DrawIgnoreFolder())
                {
                    maskDirty();
                }
            }
            else if (FR2_Setting.showSettings)
            {
                FR2_Setting.s.DrawSettings();
            }
        }
        public bool WillRepaint;
        private bool showFilter, showSetting, showIgnore;
        private void maskDirty()
        {
            UsedByDrawer.SetDirty();
            SceneUsesDrawer.SetDirty();
            UsesDrawer.SetDirty();
            SceneToAssetDrawer.SetDirty();
            Duplicated.SetDirty();
            RefInScene.SetDirty();
            RefSceneInScene.SetDirty();
        }
        private void RefreshSort()
        {
            SceneUsesDrawer.RefreshSort();
            UsedByDrawer.RefreshSort();
            UsesDrawer.RefreshSort();
            SceneToAssetDrawer.RefreshSort();
            Duplicated.RefreshSort();
            RefInScene.RefreshSort();
            RefSceneInScene.RefreshSort();
        }
        // public bool isExcludeByFilter;
        private bool AnyToolInBot
        {
            get
            {
                return FR2_Setting.showSettings || showIgnore || showFilter;
            }
        }
        private bool checkNoticeFilter()
        {
            bool rsl = false;
            if(IsFocusingDuplicate) return Duplicated.isExclueAnyItem();
            
            if(IsFocusingUses && rsl == false)  rsl = UsesDrawer.isExclueAnyItem();
            if(IsFocusingSceneToAsset && rsl == false)  rsl = SceneToAssetDrawer.isExclueAnyItem();

            //tab use by
            if(IsFocusingSceneToAsset && !rsl)                  rsl = SceneToAssetDrawer.isExclueAnyItem();
            if(IsFocusingFindInScene && !rsl)           rsl =  RefInScene.isExclueAnyItem();
            if(IsFocusingSceneInScene && !rsl)          rsl =  RefSceneInScene.isExclueAnyItem();
            if(IsFocusingUsedBy && !rsl)                rsl =  UsedByDrawer.isExclueAnyItem();
            return rsl;
        }
        private bool checkNoticeIgnore()
        {
            bool rsl = isNoticeIgnore;
            if(!rsl && IsFocusingDuplicate) return Duplicated.isExclueAnyItemByIgnoreFolder();
            return rsl;
        }
        private void DrawFooter()
        {
           


            GUILayout.FlexibleSpace();


            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                if (FR2_Unity.DrawToggleToolbar(ref FR2_Setting.showSettings, "*", 20f))
                {
                    maskDirty();
                    if (FR2_Setting.showSettings) showFilter = showIgnore = false;
                }
                bool v = checkNoticeFilter();
                string content = !FR2_Setting.IsIncludeAllType() ? "*Filter" : "Filter";
                if (v)
                {
                    var oc = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    v = GUILayout.Toggle(showFilter, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
                    GUI.backgroundColor = oc;
                }
                else
                {
                    
                    v = GUILayout.Toggle(showFilter, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
                }

                if (v != showFilter)
                {
                    showFilter = v;
                    if (showFilter) FR2_Setting.showSettings = showIgnore = false;
                }

                v = checkNoticeIgnore();
                content = FR2_Setting.IgnoreAsset.Count > 0 ? "*Ignore" : "Ignore"; 
                if (v)
                {
                    var oc = GUI.backgroundColor;
                    GUI.backgroundColor = Color.red;
                    v = GUILayout.Toggle(showIgnore, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
                    GUI.backgroundColor = oc;
                }
                else
                {
                    
                    v = GUILayout.Toggle(showIgnore, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
                }
                // var i = GUILayout.Toggle(showIgnore, content, EditorStyles.toolbarButton, GUILayout.Width(50f));
                if (v != showIgnore)
                {
                    showIgnore = v;
                    if (v) showFilter = FR2_Setting.showSettings = false;
                }

                var ss = FR2_Setting.ShowSelection;
                v = GUILayout.Toggle(ss, "Selection", EditorStyles.toolbarButton, GUILayout.Width(60f));
                if (v != ss)
                {
                    FR2_Setting.ShowSelection = v;
                    maskDirty();
                }
                if(FR2_Selection.h.Count > 0)
                {
                    if (GUILayout.Button("Commit Selection [" + FR2_Selection.h.Count + "]", EditorStyles.toolbarButton))
                    {
                        FR2_Selection.Commit();
                    }

                    if (GUILayout.Button("Clear Selection", EditorStyles.toolbarButton))
                    {
                        FR2_Selection.h.Clear();
                    }
                }
                

                GUILayout.FlexibleSpace();


                if(!IsFocusingDuplicate && !IsFocusingGUIDs)
                {
                    var o = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 42f;

                    var ov = FR2_Setting.GroupMode;
                    var vv = (vietlabs.fr2.FR2_RefDrawer.Mode)EditorGUILayout.EnumPopup("Group", ov, GUILayout.Width(122f));
                    if (vv != ov)
                    {
                        FR2_Setting.GroupMode = vv;
                        maskDirty();
                    }

                    GUILayout.Space(4f);
                    EditorGUIUtility.labelWidth = 30f;

                    var s = FR2_Setting.SortMode;
                    var vvv = (vietlabs.fr2.FR2_RefDrawer.Sort)EditorGUILayout.EnumPopup("Sort", s, GUILayout.Width(100f));
                    if (vvv != s)
                    {
                        FR2_Setting.SortMode = vvv;
                        RefreshSort();
                    }

                    EditorGUIUtility.labelWidth = o;
                }
                
            }

            GUILayout.EndHorizontal();

        }
        public Rect GetTopPanelRect()
        {
            return new Rect(0, 17, position.width, position.height * sizeRatio - 20);
        }
        public Rect GetBotPanelRect()
        {
            return new Rect(0, (position.height * sizeRatio) + 5, position.width, position.height * (1 - sizeRatio) - 5 - paddingBot);
        }
        private void DrawResizer()
        {
            resizer = new Rect(0, (position.height * sizeRatio) - 5f, position.width, 10f);

            Vector2 a =resizer.position + (Vector2.up * 5f);
            GUILayout.BeginArea(new Rect(a.x, a.y , position.width,1), resizerStyle);
            // GUILayout.BeginArea(new Rect(resizer.position + (Vector2.up * 5f), new Vector2(position.width, 1)), resizerStyle);
            GUILayout.EndArea();

            EditorGUIUtility.AddCursorRect(resizer, MouseCursor.ResizeVertical);
        }
        private void ProcessEvents(Event e)
        {
            switch (e.type)
            {
                case UnityEngine.EventType.MouseDown:
                    if (e.button == 0 && resizer.Contains(e.mousePosition))
                    {
                        isResizing = true;
                    }
                    break;
                #if UNITY_5_3_OR_NEWER
                case UnityEngine.EventType.MouseLeaveWindow:
                #endif
                case UnityEngine.EventType.MouseUp:
                    isResizing = false;
                    break;
            }

            Resize(e);
        }
        private void Resize(Event e)
        {
            if (isResizing)
            {
                sizeRatio = e.mousePosition.y / position.height;
                normalizeSize();
                Repaint();
            }
        }
        const float paddingTop = 70;
        const float paddingBot = 25;
        private void normalizeSize()
        {
            if (sizeRatio * position.height < paddingTop) sizeRatio = paddingTop / position.height;
            if (position.height - (sizeRatio * position.height) < paddingBot)
                sizeRatio = (position.height - paddingBot) * 1f / position.height;

        }

        Object[] objs;
        string[] ids;
        private void DrawGUIDs()
        {
            
            GUILayout.Label("GUID to Object", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            {
                var guid = EditorGUILayout.TextField(tempGUID ?? string.Empty);
                EditorGUILayout.ObjectField(tempObject, typeof(Object), false, GUILayout.Width(120f));

                if (GUILayout.Button("Paste", EditorStyles.miniButton, GUILayout.Width(70f)))
                {
                    guid = EditorGUIUtility.systemCopyBuffer;
                }

                if (guid != tempGUID && !string.IsNullOrEmpty(guid))
                {
                    tempGUID = guid;

                    tempObject = FR2_Unity.LoadAssetAtPath<Object>
                    (
                        AssetDatabase.GUIDToAssetPath(tempGUID)
                    );
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            if(objs == null || ids == null) return;

            //GUILayout.Label("Selection", EditorStyles.boldLabel);
            if (ids.Length == objs.Length)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                {
                    for (var i = 0; i < ids.Length; i++)
                    {
                        GUILayout.BeginHorizontal();
                        {
                            EditorGUILayout.ObjectField(objs[i], typeof(Object), false);
                            var idi = ids[i];
                            GUILayout.TextField(idi, GUILayout.Width(240f));
                            if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(50f)))
                            {
                                EditorGUIUtility.systemCopyBuffer =tempGUID= idi;

                                if (!string.IsNullOrEmpty(tempGUID))
                                {

                                    tempObject = FR2_Unity.LoadAssetAtPath<Object>
                                    (
                                        AssetDatabase.GUIDToAssetPath(tempGUID)
                                    );
                                }
                                //Debug.Log(EditorGUIUtility.systemCopyBuffer);  
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
            }

            // GUILayout.FlexibleSpace();
            // GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Merge Selection To"))
            {
                FR2_Export.MergeDuplicate();
                
            }
            EditorGUILayout.ObjectField(tempObject, typeof(Object), false, GUILayout.Width(120f));
            GUILayout.EndHorizontal();
             GUILayout.Space(5f);
        }

        private void DrawUnused()
        {
            if (unusedArray == null) unusedArray = new List<FR2_Asset>();
            if (unusedScroller == null) unusedScroller = new FR2_TreeUI();

            unusedScroller.Draw(unusedArray.Count, (idx, r, s) =>
            {
                var item = unusedArray[idx];
                item.Draw(r, false, false);
            });
        }
    }
}