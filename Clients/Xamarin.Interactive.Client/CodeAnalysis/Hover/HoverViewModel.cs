using Microsoft.CodeAnalysis.Text;

namespace Xamarin.Interactive.CodeAnalysis.Hover
{
    sealed class HoverViewModel
    {
        public string [] Contents { get; set; }
        public LinePositionSpan Range { get; set; }
    }
}