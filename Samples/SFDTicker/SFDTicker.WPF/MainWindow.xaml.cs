using Xamarin.Forms;
using Xamarin.Forms.Platform.WPF;

namespace SFDTicker.WPF
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : FormsApplicationPage
    {
        public MainWindow()
        {
            InitializeComponent();

            Forms.Init();
            LoadApplication(new SFDTicker.App());
        }
    }
}
