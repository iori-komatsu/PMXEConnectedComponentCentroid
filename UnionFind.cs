namespace PMXEConnectedComponentCentroid {
    class UnionFind {
        private readonly int[] data;

        public UnionFind(int size) {
            data = new int[size];
            for (int i = 0; i < data.Length; i++) {
                data[i] = -1;
            }
        }

        public int Root(int x) {
            if (data[x] < 0) {
                return x;
            } else {
                data[x] = Root(data[x]);
                return data[x];
            }
        }

        public int Size(int x) {
            return -data[Root(x)];
        }

        public bool Merge(int x, int y) {
            x = Root(x);
            y = Root(y);
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
