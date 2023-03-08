using System.Windows.Controls;

namespace TeramedQRTool.Interface
{
    public interface IPage
    {
        string Name { get; set; }

        string Icon { get; set; }

        UserControl UserControl { get; set; }
    }
}