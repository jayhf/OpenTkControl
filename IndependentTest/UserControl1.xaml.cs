using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
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

namespace IndependentTest
{
    /// <summary>
    /// UserControl1.xaml 的交互逻辑
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        public UserControl1()
        {
            InitializeComponent();
            this.IsVisibleChanged += UserControl1_IsVisibleChanged;
            CompositionTarget.Rendering += CompositionTarget_Rendering;
            Task.Run(async ()=>{
                await Task.Delay(1000);
                var x= this.Width.ToString(); 
                Debug.WriteLine(x);
                }

                );
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
           
        }

        private void UserControl1_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
           
            
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
           
            base.OnRender(drawingContext);
        }
    }
}
