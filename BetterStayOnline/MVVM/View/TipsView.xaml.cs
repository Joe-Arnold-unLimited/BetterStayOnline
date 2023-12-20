using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BetterStayOnline.MVVM.View
{
    /// <summary>
    /// Interaction logic for TipsView.xaml
    /// </summary>
    public partial class TipsView : UserControl, INotifyPropertyChanged
    {
        private string _path;

        public string Path
        {
            get { return _path; }
            private set
            {
                if (_path != value)
                {
                    _path = value;
                    OnPropertyChanged(nameof(Path));
                }
            }
        }

        public TipsView()
        {
            InitializeComponent();
            Path = AppDomain.CurrentDomain.BaseDirectory + "\\External\\Tips.pdf";
            DataContext = this; // Set the DataContext to enable data binding
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
