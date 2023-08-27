namespace GameDataServer
{
    public class PlayerProfile
    {
        private int id;
        private string name;
        private string pubKey;
        private bool needsUpdate = false;

        public int Id {
            get { return id; }
            set { id = value; needsUpdate = true; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; needsUpdate = true; }
        }

        public string PublicKey
        {
            get { return pubKey; }
            set { pubKey = value; needsUpdate = true; }
        }

        public bool NeedsUpdate
        {
            get { return needsUpdate; }
        }

        public PlayerProfile(int id, string pubKey, string name)
        {
            this.id = id;
            this.name = name;
            this.pubKey = pubKey;
        }

        public PlayerProfile Clone()
        {
            return new PlayerProfile(id, pubKey, name);
        }

        public void Reset()
        {
            needsUpdate = false;
        }
    }
}
