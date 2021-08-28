namespace PMXEConnectedComponentCentroid {
    class UnionFind {
        private readonly int[] data;

        public UnionFind(int size) {
            data = new int[size];
            for (int i = 0; i < data.Length; i++) {
                data[i] = -1;
            }
        }

        // x が属する集合の代表を返す
        public int Repr(int x) {
            if (data[x] < 0) {
                return x;
            } else {
                data[x] = Repr(data[x]);
                return data[x];
            }
        }

        // x が属する集合のサイズを返す
        public int Size(int x) {
            return -data[Repr(x)];
        }

        // x が属する集合と y が属する集合を合併する
        public bool Merge(int x, int y) {
            x = Repr(x);
            y = Repr(y);
            if (x != y) {
                if (data[y] < data[x]) {
                    int tmp = x;
                    x = y;
                    y = tmp;
                }
                data[x] += data[y];
                data[y] = x;
            }
            return x != y;
        }
    }
}
