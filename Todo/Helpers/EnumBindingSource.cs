using System;
using System.Windows.Markup;

namespace Todo.Helpers
{
    public class EnumBindingSource : MarkupExtension
    {
        public Type? EnumType { get; set; }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (EnumType == null || !EnumType.IsEnum)
                throw new ArgumentException("EnumType must not be null and of type Enum");

            return Enum.GetValues(EnumType);
        }
    }
}
