﻿using EngineNS.EGui.Controls;
using System;
using System.Collections.Generic;
using System.Text;

namespace EngineNS.Editor.Forms
{
    public class UWorldOutliner : UTreeNodeDrawer, IRootForm
    {
        public GamePlay.UWorld World
        {
            get
            {
                return WorldViewportState.World;
            }
        }
        public EGui.Slate.UWorldViewportSlate WorldViewportState { get; set; }
        public UWorldOutliner(EGui.Slate.UWorldViewportSlate viewport, bool regRoot = true)
        {
            WorldViewportState = viewport;
            if (regRoot)
                UEngine.RootFormManager.RegRootForm(this);

            UpdateAddNodeMenu();
        }

        public void Dispose()
        {

        }
        //List<EGui.UIProxy.MenuItemProxy> mDirContextMenu;
        public virtual async System.Threading.Tasks.Task<bool> Initialize()
        {
            await Thread.TtAsyncDummyClass.DummyFunc();
            //mDirContextMenu = new List<EGui.UIProxy.MenuItemProxy>()
            //{
            //    new EGui.UIProxy.MenuItemProxy()
            //    {
            //        MenuName = "Goto",
            //        Action = (item, data)=>
            //        {
            //            var node = data.Value.ToObject() as GamePlay.Scene.UNode;
            //            var camera = WorldViewportState.CameraController.Camera;
            //            var radius = (node.AABB.GetMaxSide()) *  5.0f;
            //            camera.LookAtLH(node.Placement.Position - camera.GetDirection().AsDVector() * radius, node.Placement.Position, Vector3.Up);
            //        },
            //    },
            //    new EGui.UIProxy.MenuItemProxy()
            //    {
            //        MenuName = "DoCommand",
            //        Action = (item, data)=>
            //        {
            //            var node = data.Value.ToObject() as GamePlay.Scene.UNode;
            //            node.OnCommand("WorldOutliner");
            //        },
            //    },
            //    new EGui.UIProxy.MenuItemProxy()
            //    {
            //        MenuName = "Delete",
            //        Action = (item, data)=>
            //        {
            //            var node = data.Value.ToObject() as GamePlay.Scene.UNode;
            //            node.Parent = null;
            //        },
            //    },
            //};

            return true;
        }
        public string Title { get; set; } = "WorldOutliner";
        public bool Visible { get; set; } = true;
        public uint DockId { get; set; }
        public ImGuiWindowClass DockKeyClass { get; }
        public ImGuiCond_ DockCond { get; set; } = ImGuiCond_.ImGuiCond_FirstUseEver;
        public virtual unsafe void DrawAsChildWindow(in Vector2 size)
        {
            if (ImGuiAPI.BeginChild(Title, in size, true, ImGuiWindowFlags_.ImGuiWindowFlags_None))
            {
                if (ImGuiAPI.IsWindowDocked())
                {
                    DockId = ImGuiAPI.GetWindowDockID();
                }
                if (World != null)
                {
                    DrawTree(World.Root, 0);
                }
            }
            if (OnDrawMenu != null)
                OnDrawMenu();
            ImGuiAPI.EndChild();

            
        }
        public unsafe void OnDraw()
        {
            if (Visible == false)
                return;
            ImGuiAPI.SetNextWindowDockID(DockId, DockCond);
            if (EGui.UIProxy.DockProxy.BeginMainForm(Title, this, ImGuiWindowFlags_.ImGuiWindowFlags_None))
            {
                if (ImGuiAPI.IsWindowDocked())
                {
                    DockId = ImGuiAPI.GetWindowDockID();
                }
                if (World != null)
                {
                    DrawTree(World.Root, 0);
                }
            }
            if (OnDrawMenu != null)
                OnDrawMenu();
            EGui.UIProxy.DockProxy.EndMainForm();
        }
        public List<INodeUIProvider> SelectedNodes = new List<INodeUIProvider>();
        protected override void AfterNodeShow(INodeUIProvider provider, int index)
        {
            if (ImGuiAPI.IsItemActivated())
            {
                OnNodeUI_Activated(provider);
                //if (ImGuiAPI.IsKeyDown((int)SDL2.SDL.SDL_Scancode.SDL_SCANCODE_DELETE))
                //{
                //    int xxx = 0;
                //}
            }
            if (ImGuiAPI.IsItemDeactivated())
            {
            }
            if (ImGuiAPI.IsItemClicked(ImGuiMouseButton_.ImGuiMouseButton_Left))
            {
                OnNodeUI_LClick(provider);
            }
            if (ImGuiAPI.IsItemClicked(ImGuiMouseButton_.ImGuiMouseButton_Right))
            {
                OnNodeUI_RClick(provider);
            }
            if (ImGuiAPI.IsItemHovered(ImGuiHoveredFlags_.ImGuiHoveredFlags_None) && ImGuiAPI.IsMouseDown(ImGuiMouseButton_.ImGuiMouseButton_Left))
            {
                //这里考虑一下单选多选的问题
                if (ImGuiAPI.IsKeyDown((ImGuiKey)Bricks.Input.Scancode.SCANCODE_LCTRL))
                {
                    provider.Selected = !provider.Selected;
                    if (provider.Selected == false)
                    {
                        SelectedNodes.Remove(provider);
                    }
                    else
                    {
                        if (SelectedNodes.Contains(provider) == false)
                        {
                            SelectedNodes.Add(provider);
                        }
                    }
                }
                else
                {
                    foreach (var i in SelectedNodes)
                    {
                        i.Selected = false;
                    }
                    SelectedNodes.Clear();
                    provider.Selected = true;
                    SelectedNodes.Add(provider);
                }
            }   
        }
        protected override bool OnDrawNode(INodeUIProvider provider, int index, int NumOfChild)
        {
            return provider.DrawNode(this, index, NumOfChild);
            //ImGuiTreeNodeFlags_ flags = 0;
            //if (provider.Selected)
            //    flags = ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_Selected;
            //var ret = ImGuiAPI.TreeNodeEx(index.ToString(), flags, "");
            //ImGuiAPI.SameLine(0, -3);
            //ImGuiAPI.Text(provider.NodeName);
            //return ret;
        }
        #region PopMenu
        System.Action OnDrawMenu = null;
        GamePlay.Scene.UNode mAddToNode;
        bool mNodeMenuShow = false;
        bool mAddNodeMenuFilterFocused = false;
        string mAddNodeMenuFilterStr = "";
        public TtMenuItem mAddNodeMenus = new TtMenuItem();
        static void GetNodeNameAndMenuStr(in string menuString, ref string nodeName, ref string menuName)
        {
            menuName = menuString;
            nodeName = menuName;
        }
        private async System.Threading.Tasks.Task<GamePlay.Scene.UNode> NewNode(Rtti.UClassMeta i)
        {
            if (mAddToNode == null)
                return null;
            var ntype = Rtti.UTypeDesc.TypeOf(i.ClassType.TypeString);
            var newNode = Rtti.UTypeDescManager.CreateInstance(ntype) as GamePlay.Scene.USceneActorNode;
            var attrs = newNode.GetType().GetCustomAttributes(typeof(GamePlay.Scene.UNodeAttribute), false);
            GamePlay.Scene.UNodeData nodeData = null;
            string prefix = "Node";
            if (attrs.Length > 0)
            {
                nodeData = Rtti.UTypeDescManager.CreateInstance((attrs[0] as GamePlay.Scene.UNodeAttribute).NodeDataType) as GamePlay.Scene.UNodeData;
                prefix = (attrs[0] as GamePlay.Scene.UNodeAttribute).DefaultNamePrefix;
            }
            await newNode.InitializeNode(World, nodeData, GamePlay.Scene.EBoundVolumeType.Box, typeof(GamePlay.UPlacement));
            newNode.NodeData.Name = $"{prefix}_{newNode.SceneId}";
            newNode.Parent = mAddToNode;
            return newNode;
        }
        public void UpdateAddNodeMenu()
        {
            mAddNodeMenus = new TtMenuItem();
            var typeDesc = Rtti.UTypeDescGetter<GamePlay.Scene.UNode>.TypeDesc;
            var meta = Rtti.TtClassMetaManager.Instance.GetMeta(typeDesc);
            var subClasses = meta.SubClasses;
            foreach (var i in subClasses)
            {
                var atts = i.ClassType.SystemType.GetCustomAttributes(typeof(Bricks.CodeBuilder.ContextMenuAttribute), true);
                if (atts.Length > 0)
                {
                    var parentMenu = mAddNodeMenus;
                    var att = atts[0] as Bricks.CodeBuilder.ContextMenuAttribute;

                    if (!att.HasKeyString(GamePlay.Scene.UNode.EditorKeyword))
                        continue;

                    for (var menuIdx = 0; menuIdx < att.MenuPaths.Length; menuIdx++)
                    {
                        var menuStr = att.MenuPaths[menuIdx];
                        string nodeName = null;
                        GetNodeNameAndMenuStr(menuStr, ref nodeName, ref menuStr);
                        if (menuIdx < att.MenuPaths.Length - 1)
                            parentMenu = parentMenu.AddMenuItem(menuStr, null, null);
                        else
                        {
                            parentMenu.AddMenuItem(menuStr, att.FilterStrings, null,
                                (item, sender) =>
                                {
                                    var nu = NewNode(i);
                                });
                        }
                    }
                }
            }
        }
        private void DrawMenu(TtMenuItem item, string filter = "")
        {
            if (!item.FilterCheck(filter))
                return;

            if (item.OnMenuDraw != null)
            {
                item.OnMenuDraw(item, this);
                return;
            }

            if (item.SubMenuItems.Count == 0)
            {
                if (!string.IsNullOrEmpty(item.Text))
                {
                    ImGuiAPI.TreeNodeEx(item.Text, ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_Leaf | ImGuiTreeNodeFlags_.ImGuiTreeNodeFlags_NoTreePushOnOpen);
                    if (ImGuiAPI.IsItemClicked(ImGuiMouseButton_.ImGuiMouseButton_Left))
                    {
                        if (item.Action != null)
                        {
                            item.Action(item, null);
                            ImGuiAPI.CloseCurrentPopup();
                        }
                    }
                }
            }
            else
            {
                if (ImGuiAPI.TreeNode(item.Text))
                {
                    for (int menuIdx = 0; menuIdx < item.SubMenuItems.Count; menuIdx++)
                    {
                        DrawMenu(item.SubMenuItems[menuIdx], filter);
                    }
                    ImGuiAPI.TreePop();
                }
            }
        }
        #endregion

        protected override void OnNodeUI_RClick(INodeUIProvider provider)
        {
            var node = provider as GamePlay.Scene.UNode;
            if (node == null)
            {
                mNodeMenuShow = false;
                OnDrawMenu = null;
                return;
            }
            var scene = provider as GamePlay.Scene.UScene;
            OnDrawMenu = async () =>
            {
                if (ImGuiAPI.BeginPopupContextWindow(null, ImGuiPopupFlags_.ImGuiPopupFlags_MouseButtonRight))
                {
                    mAddToNode = node;
                    mNodeMenuShow = true;
                    DrawBaseMenu(node);
                    ImGuiAPI.EndPopup();
                }
                else
                {
                    //mAddToNode = null;
                    if (mNodeMenuShow)
                    {
                        OnDrawMenu = null;
                    }
                    mNodeMenuShow = false;
                }
            };
        }

        protected virtual void DrawBaseMenu(GamePlay.Scene.UNode node)
        {
            if (ImGuiAPI.MenuItem($"Goto", null, false, true))
            {
                var camera = WorldViewportState.CameraController.Camera;
                var radius = (node.AABB.GetMaxSide()) * 5.0f;
                camera.LookAtLH(node.Placement.Position - camera.GetDirection().AsDVector() * radius, node.Placement.Position, Vector3.Up);
            }
            if (ImGuiAPI.MenuItem($"DoCommand", null, false, true))
            {
                node.OnCommand("WorldOutliner");
            }
            if (ImGuiAPI.BeginMenu("AddChild", true))
            {
                var drawList = ImGuiAPI.GetWindowDrawList();
                EGui.UIProxy.SearchBarProxy.OnDraw(ref mAddNodeMenuFilterFocused, in drawList, "search item", ref mAddNodeMenuFilterStr, -1);
                for (var childIdx = 0; childIdx < mAddNodeMenus.SubMenuItems.Count; childIdx++)
                    DrawMenu(mAddNodeMenus.SubMenuItems[childIdx], mAddNodeMenuFilterStr.ToLower());

                ImGuiAPI.EndMenu();
            }
            if (ImGuiAPI.MenuItem($"Delete", null, false, true))
            {
                if (World.Root != node)
                {
                    node.Parent = null;
                }
            }
        }
        protected override void OnNodeUI_LClick(INodeUIProvider provider)
        {
            //var appliction = UEngine.Instance.GfxDevice.MainWindow as EngineNS.Editor.UMainEditorApplication;
            //if (appliction == null)
            //    return;
            //appliction.mMainInspector.PropertyGrid.Target = provider;
            //appliction.WorldViewportSlate.OnHitproxySelected((GamePlay.Scene.UNode)provider);
            WorldViewportState.OnHitproxySelected((GamePlay.Scene.UNode)provider);
        }
    }
}
