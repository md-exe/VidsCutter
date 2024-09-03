using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Path = System.IO.Path;

namespace VidsCutter
{
    public partial class MainWindow : Window
    {
        private List<string> selectedFiles = new List<string>();

        public MainWindow()
        {
            InitializeComponent();
        }

        // Область для видео
        private void DnDZone_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Video Files|*.mp4;*.avi";
            openFileDialog.Multiselect = true;
            if (openFileDialog.ShowDialog() == true)
            {
                string[] fileNames = openFileDialog.FileNames;
                int fileCount = fileNames.Length;
                Counter(fileCount);
            }
            else
            {
                Counter(0);
            }
        }

        // Только числа для ввода секунд
        private void SecondValueTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            if (!char.IsDigit(e.Text, e.Text.Length - 1))
            {
                e.Handled = true;
                return;
            }

            // Проверка ведущего нуля
            string newText = textBox.Text.Insert(textBox.CaretIndex, e.Text);
            if (newText.Length > 1 && newText.StartsWith("0"))
            {
                e.Handled = true;
                return;
            }
        }

        private void SecondValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            if (string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = "";
            }
            else
            {
                if (!int.TryParse(textBox.Text, out int seconds))
                {
                    textBox.Text = "1";
                }
                else if (seconds == 0)
                {
                    textBox.Text = "";
                }
            }
        }

        // Drag n Drop
        private void DnDZone_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                int fileCount = files.Length;
                Counter(fileCount);

                selectedFiles.AddRange(files);
            }
        }

        private void DnDZone_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        private void DnDZone_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
        }

        // Счётчик видео
        private void Counter(int fileCount)
        {
            FilesCountLabel.Text = "Видео выбрано: " + fileCount.ToString();
        }

        // Обрезать видео
        public async Task CutVideoAsync(string inputFilePath, int secondsToKeep)
        {
            string outputFileName = "Cut " + Path.GetFileNameWithoutExtension(inputFilePath);
            string outputFilePath = Path.Combine(Path.GetDirectoryName(inputFilePath), outputFileName + Path.GetExtension(inputFilePath));

            await Task.Run(() =>
            {
                try
                {
                    using (var engine = new Engine())
                    {
                        var inputFile = new MediaFile { Filename = inputFilePath };
                        var outputFile = new MediaFile { Filename = outputFilePath };

                        engine.GetMetadata(inputFile);

                        var duration = inputFile.Metadata.Duration;
                        var startTime = duration - TimeSpan.FromSeconds(secondsToKeep);

                        var conversionOptions = new ConversionOptions { Seek = startTime };

                        engine.Convert(inputFile, outputFile, conversionOptions);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при обрезке видео: " + ex.Message, "Технические неполадки", MessageBoxButton.OK, MessageBoxImage.Error,MessageBoxResult.None);
                }
            });
        }

        private void Readiness()
        {
            selectedFiles.Clear(); // Очищаем список после обработки всех файлов
            CutButton.IsEnabled = true; // Делаем кнопку активной снова
            DnDZone.IsEnabled = true;
            FilesCountLabel.Text = "Видео выбрано: 0";
        }

        // Вызвать функции урезания видео
        private async void CutButton_Click(object sender, RoutedEventArgs e)
        {
            int secondsToKeep;
            if (int.TryParse(SecondValueTextBox.Text, out secondsToKeep) && FilesCountLabel.Text != "Видео выбрано: 0")
            {
                CutButton.IsEnabled = false; // Делаем кнопку неактивной
                DnDZone.IsEnabled = false;

                if (CheckBoxBtn.IsChecked == true)
                {
                    var confirmDelete = MessageBox.Show("Файлы будут удалены без возможности восстановления.","Внимание!",MessageBoxButton.OKCancel,MessageBoxImage.Warning);
                    if (confirmDelete == MessageBoxResult.OK)
                    {
                        foreach (string filePath in selectedFiles)
                        {
                            await CutVideoAsync(filePath, secondsToKeep);
                            File.Delete(filePath);
                        }
                    }
                    else
                    {
                        Readiness();
                        return;
                    }
                }
                else
                {
                    foreach (string filePath in selectedFiles)
                    {
                        await CutVideoAsync(filePath, secondsToKeep);
                    }
                }
                MessageBox.Show("Обрезка видео завершена!", "Успешный успех", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.None);
                Readiness();
            }
            else
            {
                MessageBox.Show("Сначала выберите видео.", "Остановись", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.None);
            }
        }
    }
}

