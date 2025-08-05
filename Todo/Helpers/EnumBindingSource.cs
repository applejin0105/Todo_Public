using System.Windows.Markup;

// 애플리케이션의 헬퍼(도우미) 클래스를 포함하는 네임스페이스
namespace Todo.Helpers
{
    // XAML에서 열거형(Enum)을 데이터 바인딩 소스로 쉽게 사용할 수 있도록 하는 사용자 정의 마크업 확장(Markup Extension) 클래스
    public class EnumBindingSource : MarkupExtension
    {
        // XAML에서 바인딩할 열거형의 타입을 지정하는 속성
        public Type? EnumType { get; set; }

        // 마크업 확장이 XAML 파서에 의해 실행될 때 호출되는 핵심 메서드
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            // EnumType 속성이 설정되지 않았거나, 설정된 타입이 열거형이 아닌 경우 예외를 발생
            if (EnumType == null || !EnumType.IsEnum)
                throw new ArgumentException("EnumType must not be null and of type Enum");

            // 지정된 열거형의 모든 값들을 배열 형태로 반환. 이 값들은 ComboBox 등의 ItemsSource로 사용됨
            return Enum.GetValues(EnumType);
        }
    }
}