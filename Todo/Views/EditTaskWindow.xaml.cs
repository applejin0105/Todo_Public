using MahApps.Metro.Controls;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace Todo.Views
{
    using Todo.Data;
    using Todo.Models;
    using Todo.ViewModels;

    public partial class EditTaskWindow : MetroWindow
    {
        public TodoItem EditingItem { get; }
        private readonly AppDbContext _context;
        private readonly MainViewModel _mainViewModel; // MainViewModel 참조

        // 시간 입력을 위한 속성
        public string StartTime { get; set; }
        public string DueTime { get; set; }

        // 플랫폼 목록을 위한 속성
        public ObservableCollection<Platform> AllPlatforms => _mainViewModel.AllPlatforms;

        public EditTaskWindow(TodoItem item, AppDbContext context, MainViewModel mainViewModel)
        {
            InitializeComponent();
            EditingItem = item;
            _context = context;
            _mainViewModel = mainViewModel; // MainViewModel 인스턴스 저장

            // 기존 시간 값을 텍스트박스용 속성으로 변환
            StartTime = EditingItem.StartDate?.ToLocalTime().ToString("HH:mm") ?? "";
            DueTime = EditingItem.DueDate?.ToLocalTime().ToString("HH:mm") ?? "";

            DataContext = this;
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 수정된 시간 텍스트를 다시 DateTime으로 변환하여 저장
            EditingItem.StartDate = CombineDateAndTime(EditingItem.StartDate, StartTime)?.ToUniversalTime();
            EditingItem.DueDate = CombineDateAndTime(EditingItem.DueDate, DueTime)?.ToUniversalTime();

            await _context.SaveChangesAsync();
            this.DialogResult = true;
            this.Close();
        }

        private DateTime? CombineDateAndTime(DateTime? date, string timeString)
        {
            if (date is null) return null;
            if (string.IsNullOrWhiteSpace(timeString)) return date.Value.Date;
            if (TimeSpan.TryParse(timeString, out var time)) return date.Value.Date + time;
            return date.Value.Date;
        }

        private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                SaveButton_Click(sender, e);
            }
        }

    }
}