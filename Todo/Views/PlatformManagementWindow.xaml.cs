using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using MahApps.Metro.Controls;
using Microsoft.EntityFrameworkCore;

namespace Todo.Views
{
    using Todo.Data;
    using Todo.Models;

    public partial class PlatformManagementWindow : MetroWindow
    {
        private readonly AppDbContext _context;

        public PlatformManagementWindow(AppDbContext context)
        {
            InitializeComponent();
            _context = context;
            LoadPlatforms();
        }

        private void LoadPlatforms()
        {
            PlatformListView.ItemsSource = _context.Platforms.OrderBy(p => p.Name).ToList();
        }

        private async void AddPlatform_Click(object sender, RoutedEventArgs e)
        {
            var newName = NewPlatformTextBox.Text;
            if (!string.IsNullOrWhiteSpace(newName))
            {
                if (!await _context.Platforms.AnyAsync(p => p.Name == newName))
                {
                    _context.Platforms.Add(new Platform { Name = newName });
                    await _context.SaveChangesAsync();
                    NewPlatformTextBox.Clear();
                    LoadPlatforms();
                }
                else
                {
                    MessageBox.Show("Already exist Platform.");
                }
            }
        }

        private async void DeletePlatform_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int platformId)
            {
                var platformToDelete = await _context.Platforms.FindAsync(platformId);
                if (platformToDelete != null)
                {
                    _context.Platforms.Remove(platformToDelete);
                    await _context.SaveChangesAsync();
                    LoadPlatforms();
                }
            }
        }

        private void NewPlatform_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddPlatform_Click(sender, e);
            }
        }

        private void MetroWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }

    }
}
