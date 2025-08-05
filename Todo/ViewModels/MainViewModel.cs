using Microsoft.EntityFrameworkCore;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

using Todo.Data;
using Todo.Models;
using Todo.Services;

namespace Todo.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AppDbContext _context;
        private readonly SettingsService _settingsService;
        private readonly KakaoService _kakaoService;

        public ObservableCollection<TodoItem> NotStartedItems { get; set; }
        public ObservableCollection<TodoItem> InProgressItems { get; set; }
        public ObservableCollection<TodoItem> CompletedItems { get; set; }
        public ObservableCollection<Platform> AllPlatforms { get; set; }

        public string NewItemTitle { get; set; } = "";
        public string NewItemPlatform { get; set; } = "";

        public MainViewModel(AppDbContext context, SettingsService settingsService, KakaoService kakaoService)
        {
            _context = context;
            NotStartedItems = new ObservableCollection<TodoItem>();
            InProgressItems = new ObservableCollection<TodoItem>();
            CompletedItems = new ObservableCollection<TodoItem>();
            AllPlatforms = new ObservableCollection<Platform>();
            _settingsService = settingsService;
            _kakaoService = kakaoService;
        }

        public async Task LoadItemsAsync()
        {
            var platforms = await _context.Platforms.OrderBy(p => p.Name).ToListAsync();
            AllPlatforms.Clear();
            foreach (var p in platforms)
            {
                AllPlatforms.Add(p);
            }

            var itemsFromDb = await _context.TodoItems.Include(i => i.Platform).ToListAsync();
            NotStartedItems.Clear();
            InProgressItems.Clear();
            CompletedItems.Clear();

            foreach (var item in itemsFromDb)
            {
                DistributeItem(item);
            }

            SortAllLists(item => item.DueDate ?? DateTime.MaxValue);
        }

        public async Task AddItemAsync(Platform? selectedPlatform, DateTime? startDate, DateTime? dueDate)
        {
            if (string.IsNullOrWhiteSpace(NewItemTitle)) return;

            var newItem = new TodoItem
            {
                Title = NewItemTitle,
                Platform = selectedPlatform,
                Status = WorkStatus.NotStarted,
                StartDate = startDate?.ToUniversalTime(),
                DueDate = dueDate?.ToUniversalTime()
            };

            _context.TodoItems.Add(newItem);
            await _context.SaveChangesAsync();

            NotStartedItems.Add(newItem);
            await _kakaoService.SendMessageAsync($"[작업 추가] {newItem.Title}");

            NewItemTitle = string.Empty;
            OnPropertyChanged(nameof(NewItemTitle));
        }

        public async Task UpdateItemAsync(TodoItem item)
        {
            if (item.Status == WorkStatus.Completed)
            {
                item.CompletedDate = DateTime.UtcNow;
            }
            else
            {
                item.CompletedDate = null;
            }

            _context.Update(item);
            await _context.SaveChangesAsync();

            RemoveItemFromAllLists(item);
            DistributeItem(item);
        }

        public async Task DeleteItemAsync(TodoItem item)
        {
            var itemToDelete = await _context.TodoItems.FindAsync(item.Id);
            if (itemToDelete != null)
            {
                _context.TodoItems.Remove(itemToDelete);
                await _context.SaveChangesAsync();
                RemoveItemFromAllLists(item);
            }
        }

        public void SortAllLists(Func<TodoItem, object> keySelector)
        {
            SortList(NotStartedItems, keySelector);
            SortList(InProgressItems, keySelector);
        }

        private void SortList(ObservableCollection<TodoItem> collection, Func<TodoItem, object> keySelector)
        {
            var sorted = collection.OrderBy(keySelector).ToList();
            collection.Clear();
            foreach (var item in sorted)
            {
                collection.Add(item);
            }
        }

        private void DistributeItem(TodoItem item)
        {
            switch (item.Status)
            {
                case WorkStatus.NotStarted:
                    NotStartedItems.Add(item);
                    break;
                case WorkStatus.InProgress:
                    InProgressItems.Add(item);
                    break;
                case WorkStatus.Completed:
                    CompletedItems.Add(item);
                    break;
            }
        }

        private void RemoveItemFromAllLists(TodoItem item)
        {
            if (NotStartedItems.Contains(item)) NotStartedItems.Remove(item);
            if (InProgressItems.Contains(item)) InProgressItems.Remove(item);
            if (CompletedItems.Contains(item)) CompletedItems.Remove(item);
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public async Task ReloadPlatformsAsync()
        {
            var platforms = await _context.Platforms.OrderBy(p => p.Name).ToListAsync();
            AllPlatforms.Clear();
            foreach (var p in platforms)
            {
                AllPlatforms.Add(p);
            }
        }

        public void SendStartupSummaryNotification()
        {
            if (!_settingsService.Settings.AreNotificationsEnabled) return;

            var itemsToday = NotStartedItems.Concat(InProgressItems)
                .Where(item => item.DueDate.HasValue && item.DueDate.Value.ToLocalTime().Date == DateTime.Today)
                .ToList();

            if (itemsToday.Any())
            {
                var summaryText = new System.Text.StringBuilder();
                summaryText.AppendLine($"오늘 마감 예정인 작업이 {itemsToday.Count}개 있습니다.");

                foreach (var item in itemsToday.Take(4))
                {
                    summaryText.AppendLine($"- {item.Title}");
                }

                if (itemsToday.Count > 4)
                {
                    summaryText.AppendLine("...");
                }

                var notification = new AppNotificationBuilder()
                    .AddText("📢 오늘의 할 일 목록")
                    .AddText(summaryText.ToString())
                    .BuildNotification();

                AppNotificationManager.Default.Show(notification);
            }
        }

        public void CheckForImminentNotifications()
        {
            if (!_settingsService.Settings.AreNotificationsEnabled) return;

            var now = DateTime.UtcNow;
            var imminentItems = NotStartedItems.Concat(InProgressItems)
                .Where(item => item.DueDate.HasValue &&
                               item.DueDate.Value > now &&
                               (item.DueDate.Value - now).TotalHours < 1)
                .ToList();

            foreach (var item in imminentItems)
            {
                var notification = new AppNotificationBuilder()
                    .AddText("⏰ 마감 임박!")
                    .AddText($"'{item.Title}' 작업의 마감이 1시간 이내로 남았습니다.")
                    .BuildNotification(); // AppNotification 객체 생성

                AppNotificationManager.Default.Show(notification); // 매니저를 통해 Show 호출
            }
        }

        public async Task AutoProgressTasksAsync()
        {
            var now = DateTime.UtcNow;
            var tasksToStart = NotStartedItems
                .Where(i => i.StartDate.HasValue && i.StartDate.Value <= now)
                .ToList();

            if (!tasksToStart.Any()) return;

            foreach (var item in tasksToStart)
            {
                item.Status = WorkStatus.InProgress;
                await UpdateItemAsync(item);
                await _kakaoService.SendMessageAsync($"[작업 시작] '{item.Title}' 작업이 시작되었습니다.");
            }
        }

    }
}
