using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PEPlugin;
using PEPlugin.Form;
using PEPlugin.Pmd;
using PEPlugin.Pmx;
using PEPlugin.SDX;
using PEPlugin.View;
using PEPlugin.Vmd;
using PEPlugin.Vme;

namespace PMXEConnectedComponentCentroid {
    public class CSScriptClass : PEPluginClass {

        // コンストラクタ
        public CSScriptClass() : base() {
            // 起動オプション
            // boot時実行(true/false), プラグインメニューへの登録(true/false), メニュー登録名("")
            m_option = new PEPluginOption(false, true, "連結成分の重心を書き込む");
        }

        // エントリポイント　
        public override void Run(IPERunArgs args) {
            try {
                var host = args.Host;
                var connector = host.Connector;
                var pmd = connector.Pmd.GetCurrentState();
                var pmx = connector.Pmx.GetCurrentState();
                var builder = host.Builder;

                WriteCentroids(pmd, pmx);
                AddBonesForEffect(pmx);

                // 更新を反映
                connector.Pmx.Update(pmx);
                connector.Form.UpdateList(UpdateObject.All);
                connector.View.PMDView.UpdateModel();
                connector.View.PMDView.UpdateView();

                MessageBox.Show("追加UV1に重心を書き込みました。");

            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }



        static private void WriteCentroids(IPEPmd pmd, IPXPmx pmx) {
            var unionFind = new UnionFind(pmd.Vertex.Count);

            for (int i = 0; i + 2 < pmd.Face.Count; i += 3) {
                var v1 = pmd.Face[i];
                var v2 = pmd.Face[i + 1];
                var v3 = pmd.Face[i + 2];
                unionFind.Merge(v1, v2);
                unionFind.Merge(v1, v3);
            }

            var centroid = new SlimDX.Vector3[pmd.Vertex.Count];

            // 座標を足す
            for (int i = 0; i < pmd.Vertex.Count; i++) {
                int r = unionFind.Repr(i);
                var p = pmd.Vertex[i].Position;
                centroid[r] += new SlimDX.Vector3(p.X, p.Y, p.Z);
            }

            // 連結成分のサイズで割る
            for (int i = 0; i < pmd.Vertex.Count; i++) {
                if (i == unionFind.Repr(i)) {
                    centroid[i] /= unionFind.Size(i);
                }
            }

            // 重心書き込み
            if (pmx.Header.UVACount < 1) {
                pmx.Header.UVACount = 1;
            }
            for (int i = 0; i < pmx.Vertex.Count; i++) {
                var c = centroid[unionFind.Repr(i)];
                pmx.Vertex[i].UVA1 = new V4(c.X, c.Y, c.Z, 1);
            }
        }

        static private void AddBonesForEffect(IPXPmx pmx) {
            // ボーン名の HashSet を作っておく
            var boneSet = new HashSet<string>();
            foreach (var bone in pmx.Bone) {
                boneSet.Add(bone.Name);
            }

            // ボーンを追加
            var boneNames = new [] {
                "モデル親",
                "崩壊中心",
                "崩壊速度",
                "拡散速度",
                "衝撃速度",
                "回転速度",
                "初期回転量",
                "重力ﾍﾞｸﾄﾙ",
                "崩壊開始F",
                "ﾌｪｰﾄﾞ開始F",
                "ﾌｪｰﾄﾞ期間F",
            };
            var newBones = new List<IPXBone>();
            foreach (var name in boneNames) {
                if (!boneSet.Contains(name)) {
                    var added = AddBone(pmx, name);
                    newBones.Add(added);
                }
            }

            // 表示枠に新しいボーンを追加
            var node = AddNodeIfNeeded(pmx, "エフェクト");
            foreach (var bone in newBones) {
                AddNodeItem(node, bone);
            }
        }

        static private IPXBone AddBone(IPXPmx pmx, string name) {
            var newBone = PEStaticBuilder.Pmx.Bone();
            newBone.Name = name;
            newBone.Position = new V3(0, 0, 0);
            newBone.Parent = pmx.Bone[0];
            newBone.IsRotation = false;
            newBone.IsTranslation = true;
            pmx.Bone.Add(newBone);
            return newBone;
        }

        static private IPXNode AddNodeIfNeeded(IPXPmx pmx, string name) {
            foreach (var node in pmx.Node) {
                if (node.Name == name) {
                    return node;
                }
            }
            var newNode = PEStaticBuilder.Pmx.Node();
            newNode.Name = name;
            pmx.Node.Add(newNode);
            return newNode;
        }

        static private void AddNodeItem(IPXNode node, IPXBone bone) {
            var nodeItem = PEStaticBuilder.Pmx.BoneNodeItem();
            nodeItem.Bone = bone;
            node.Items.Add(nodeItem);
        }
    }
}
