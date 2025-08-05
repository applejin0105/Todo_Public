using System.Globalization;
using System.Windows.Data;

// XAML 컨버터 클래스를 포함하는 네임스페이스
namespace Todo.Converters
{
    // UTC(협정 세계시) 시간을 사용자의 로컬 시간으로 변환하고, 특정 형식의 문자열로 만들어주는 WPF 값 변환기(Value Converter)
    public class UtcToLocalTimeConverter : IValueConverter
    {
        // 소스(데이터 모델)에서 타겟(UI)으로 값을 변환할 때 호출되는 메서드
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 입력값이 DateTime 타입이 아니면 변환하지 않고 null 반환
            if (value is not DateTime dateTime)
            {
                return null;
            }

            DateTime localTime;

            // DateTime 객체의 종류(Kind)가 지정되지 않은 경우(Unspecified)
            // 데이터베이스에서 읽어온 시간은 Kind가 지정되지 않는 경우가 많음
            if (dateTime.Kind == DateTimeKind.Unspecified)
            {
                // 해당 시간을 UTC로 간주한 후, 로컬 시간으로 변환
                localTime = DateTime.SpecifyKind(dateTime, DateTimeKind.Utc).ToLocalTime();
            }
            else
            {
                // 이미 Kind가 지정된 경우(Utc 또는 Local)에는 바로 로컬 시간으로 변환
                localTime = dateTime.ToLocalTime();
            }

            // 변환된 로컬 시간을 "yyyy-MM-dd HH:mm" 형식의 문자열로 만들어 반환
            return localTime.ToString("yyyy-MM-dd HH:mm");
        }

        // 타겟(UI)에서 소스(데이터 모델)로 값을 역변환할 때 호출되는 메서드
        public object? ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 이 컨버터는 단방향(데이터 -> UI)으로만 사용되므로, 역방향 변환은 구현하지 않음
            throw new NotImplementedException();
        }
    }
}