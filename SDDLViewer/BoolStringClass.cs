using System.Windows.Media;

namespace SDDLViewer
{
    public class BoolStringClass
    {
        public BoolStringClass(string text, string tag)
        {
            Text = text;
            Tag = tag;
            IsSelected = true;
            TextBrush = new SolidColorBrush(Color.FromRgb(0, 0, 0));
        }

        public string Text { get; }
        public string Tag { get; }
        public bool IsSelected { get; set; }
        public SolidColorBrush TextBrush { get; set; }
    }
}