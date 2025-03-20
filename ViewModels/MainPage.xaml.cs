using LosowanieUcznia.Models;

namespace LosowanieUcznia.ViewModels
{
    public partial class MainPage : ContentPage
    {
        public List<SchoolClassModel> SchoolClasses { get; set; } = new();
        public SchoolClassModel SelectedClass { get; set; }
        private Dictionary<string, Queue<StudentModel>> lastDrawnStudents = new();

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;
        }

        private async void OnImportBtnClicked(object sender, EventArgs e)
        {
            try
            {
                var file = await FilePicker.PickAsync(new PickOptions
                {
                    PickerTitle = "Wybierz plik z uczniami",
                });

                if (file != null)
                {
                    using var stream = await file.OpenReadAsync();
                    using var reader = new StreamReader(stream);
                    var fileContent = await reader.ReadToEndAsync();

                    ParseStudentData(fileContent);

                    ClassPicker.ItemsSource = null;
                    ClassPicker.ItemsSource = SchoolClasses;
                    await DisplayAlert("Sukces", "Dane uczniów zostały zaimportowane.", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", "Wystąpił problem z importem pliku: " + ex.Message, "OK");
            }
        }

        private void ParseStudentData(string fileContent)
        {
            SchoolClasses.Clear();
            lastDrawnStudents.Clear();

            var lines = fileContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool isReadingQueue = false;
            string currentClass = "";

            foreach (var line in lines)
            {
                if (line.StartsWith("["))
                {
                    if (line.Contains("Queue"))
                    {
                        currentClass = line.Replace("[", "").Replace(" Queue]", "").Trim();
                        isReadingQueue = true;
                    }
                    else
                    {
                        isReadingQueue = false;
                    }
                }
                else if (!isReadingQueue)
                {
                    var parts = line.Split(',');
                    if (parts.Length == 5)
                    {
                        var student = new StudentModel
                        {
                            FirstName = parts[0].Trim(),
                            LastName = parts[1].Trim(),
                            Number = int.Parse(parts[3].Trim()),
                            IsPresent = parts[4].Trim().ToLower() == "yes"
                        };

                        var schoolClass = SchoolClasses.FirstOrDefault(c => c.ClassName == parts[2].Trim());
                        if (schoolClass == null)
                        {
                            schoolClass = new SchoolClassModel { ClassName = parts[2].Trim() };
                            SchoolClasses.Add(schoolClass);
                        }

                        schoolClass.Students.Add(student);

                        if (!lastDrawnStudents.ContainsKey(schoolClass.ClassName))
                        {
                            lastDrawnStudents[schoolClass.ClassName] = new Queue<StudentModel>();
                        }
                    }
                }
                else
                {
                    var parts = line.Split(',');
                    if (parts.Length == 1 && int.TryParse(parts[0].Trim(), out int drawnNumber))
                    {
                        if (!string.IsNullOrEmpty(currentClass))
                        {
                            var schoolClass = SchoolClasses.FirstOrDefault(c => c.ClassName == currentClass);
                            var student = schoolClass?.Students.FirstOrDefault(s => s.Number == drawnNumber);
                            if (student != null)
                            {
                                lastDrawnStudents[currentClass].Enqueue(student);
                            }
                        }
                    }
                }
            }
        }

        private void OnClassSelected(object sender, EventArgs e)
        {
            SelectedClass = ClassPicker.SelectedItem as SchoolClassModel;
            DrawButton.IsEnabled = SelectedClass?.Students.Count > 0;
            OnPropertyChanged(nameof(SelectedClass));
        }

        private async void OnDrawButtonClicked(object sender, EventArgs e)
        {
            if (SelectedClass == null || SelectedClass.Students.Count == 0)
            {
                await DisplayAlert("Błąd", "Brak klasy lub uczniów do losowania.", "OK");
                return;
            }

            if (!lastDrawnStudents.ContainsKey(SelectedClass.ClassName))
            {
                lastDrawnStudents[SelectedClass.ClassName] = new Queue<StudentModel>();
            }

            var luckyNumber = int.TryParse(LuckyNumberEntry.Text, out int luckyNum) ? luckyNum : -1;

            var availableStudents = SelectedClass.Students
                .Where(s => s.IsPresent && s.Number != luckyNumber && !lastDrawnStudents[SelectedClass.ClassName].Contains(s))
                .ToList();

            if (availableStudents.Count > 0)
            {
                var random = new Random();
                var randomStudent = availableStudents[random.Next(availableStudents.Count)];

                lastDrawnStudents[SelectedClass.ClassName].Enqueue(randomStudent);
                if (lastDrawnStudents[SelectedClass.ClassName].Count > 3)
                {
                    lastDrawnStudents[SelectedClass.ClassName].Dequeue();
                }

                await AnimateLuckyNumber(SelectedClass.Students);

                result.Text = randomStudent.Number.ToString();
                await Task.Delay(1000);
                await DisplayAlert("Wylosowany uczeń", $"Uczeń: {randomStudent.FullName}, Numer: {randomStudent.Number}", "OK");
            }
            else
            {
                await DisplayAlert("Brak uczniów", "Brak dostępnych uczniów do losowania.", "OK");
            }
        }

        private async Task AnimateLuckyNumber(List<StudentModel> students)
        {
            var random = new Random();
            var studentNumbers = students.Select(s => s.Number).ToList();
            var originalPosition = result.TranslationY;

            for (int i = 0; i < 30; i++)
            {
                var randomNumber = studentNumbers[random.Next(studentNumbers.Count)];
                result.Text = randomNumber.ToString();

                await Task.Delay(100);
            }

            var finalNumber = studentNumbers[random.Next(studentNumbers.Count)];
            result.Text = finalNumber.ToString();
        }

        private async void OnSaveBtnClicked(object sender, EventArgs e)
        {
            try
            {
                if (SchoolClasses == null || SchoolClasses.Count == 0)
                {
                    await DisplayAlert("Błąd", "Brak danych do zapisania.", "OK");
                    return;
                }

                var fileName = "ListaUczniów.txt";
                var filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);

                var lines = new List<string>();

                foreach (var schoolClass in SchoolClasses)
                {
                    foreach (var student in schoolClass.Students)
                    {
                        lines.Add($"{student.FirstName}, {student.LastName}, {schoolClass.ClassName}, {student.Number}, {(student.IsPresent ? "yes" : "no")}");
                    }

                    if (lastDrawnStudents.ContainsKey(schoolClass.ClassName))
                    {
                        var drawnStudents = lastDrawnStudents[schoolClass.ClassName];
                        var drawnList = drawnStudents.Select(s => s.Number).ToList();
                        lines.Add($"[{schoolClass.ClassName} Queue]");
                        foreach (var studentNumber in drawnList)
                        {
                            lines.Add(studentNumber.ToString());
                        }
                    }
                }

                await File.WriteAllLinesAsync(filePath, lines);
                await DisplayAlert("Sukces", $"Lista uczniów została zapisana!\n\nŚcieżka pliku:\n{filePath}", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Błąd", $"Wystąpił problem z zapisem pliku: {ex.Message}", "OK");
            }
        }
        private async void OnEditStudentsListClicked(object sender, EventArgs e)
        {
            if (SelectedClass == null)
            {
                await DisplayAlert("Błąd", "Najpierw wybierz klasę.", "OK");
                return;
            }

            await Navigation.PushAsync(new EditStudentsList(SelectedClass));
        }
    }
}
