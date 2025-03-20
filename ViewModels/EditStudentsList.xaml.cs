using LosowanieUcznia.Models;

namespace LosowanieUcznia.ViewModels
{
    public partial class EditStudentsList : ContentPage
    {
        public SchoolClassModel SelectedClass { get; set; }

        public EditStudentsList(SchoolClassModel selectedClass)
        {
            InitializeComponent();
            SelectedClass = selectedClass;
            BindingContext = this;
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

        private void OnAddStudentClicked(object sender, EventArgs e)
        {
            string firstName = FirstNameEntry.Text?.Trim();
            string lastName = LastNameEntry.Text?.Trim();
            string numberText = NumberEntry.Text?.Trim();

            if (string.IsNullOrEmpty(firstName) || string.IsNullOrEmpty(lastName) || string.IsNullOrEmpty(numberText))
            {
                DisplayAlert("B³¹d", "Wype³nij wszystkie pola.", "OK");
                return;
            }

            if (!int.TryParse(numberText, out int number))
            {
                DisplayAlert("B³¹d", "Numer musi byæ liczb¹.", "OK");
                return;
            }

            if (SelectedClass != null)
            {
                var newStudent = new StudentModel
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Number = number,
                    IsPresent = true
                };

                SelectedClass.Students.Add(newStudent);

                StudentsListView.ItemsSource = null;
                StudentsListView.ItemsSource = SelectedClass.Students;
            }

            FirstNameEntry.Text = "";
            LastNameEntry.Text = "";
            NumberEntry.Text = "";
        }

        private void OnDeleteStudentClicked(object sender, EventArgs e)
        {
            var button = sender as Button;
            var studentToRemove = button?.CommandParameter as StudentModel;

            if (studentToRemove == null)
                return;

            SelectedClass?.Students.Remove(studentToRemove);

            StudentsListView.ItemsSource = null;
            StudentsListView.ItemsSource = SelectedClass?.Students;
        }
    }
}
