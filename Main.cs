using System;
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
                var connect = host.Connector;
                var pmd = connect.Pmd.GetCurrentState();
                var pmx = connect.Pmx.GetCurrentState();

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
                    int r = unionFind.Root(i);
                    var p = pmd.Vertex[i].Position;
                    centroid[r] += new SlimDX.Vector3(p.X, p.Y, p.Z);
                }

                // 連結成分のサイズで割る
                for (int i = 0; i < pmd.Vertex.Count; i++) {
                    if (i == unionFind.Root(i)) {
                        centroid[i] /= unionFind.Size(i);
                    }
                }

                // 重心書き込み
                if (pmx.Header.UVACount < 1) {
                    pmx.Header.UVACount = 1;
                }
                for (int i = 0; i < pmx.Vertex.Count; i++) {
                    var c = centroid[unionFind.Root(i)];
                    pmx.Vertex[i].UVA1 = new V4(c.X, c.Y, c.Z, 1);
                }

                connect.Pmx.Update(pmx);

                MessageBox.Show("追加UV1に重心を書き込みました。");

            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "エラー", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}
