namespace SpaceCore.UI
{
    public class RootElement : Container
    {
        public bool Obscured { get; set; } = false;

        public override int Width => 0;
        public override int Height => 0;

        public override void Update(bool hidden = false)
        {
            base.Update(hidden || Obscured);
            if (!hidden)
            {
                foreach (var child in Children)
                    child.Update(hidden);
            }
        }

        internal override RootElement GetRootImpl()
        {
            return this;
        }
    }
}
