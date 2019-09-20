using System.Windows.Forms;

namespace ClientApp {
    public partial class Form5 : Form {
        public Form5() {
            InitializeComponent();
        }
        public Form5(string message) {
            InitializeComponent();
            label1.Text = message;
            
            
            
        }
    }
}