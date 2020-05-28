//ESEMPIO #1: Definisco una lambda che accetta un parametro DateTime e restituisce un bool, e l'assegno alla variabile canDrive
Func<DateTime, bool> canDrive = dob => {
    return dob.AddYears(18) <= DateTime.Today;
};

//Eseguo la lambda passandole il parametro DateTime
DateTime dob = new DateTime(2002, 12, 25);
bool result = canDrive(dob);
//Poi stampo il risultato bool che ha restituito
Console.WriteLine(result);

//ESEMPIO #2: Stavolta definisco una lambda che accetta un parametro DateTime ma non restituisce nulla
Action<DateTime> printDate = date => Console.WriteLine(date);

DateTime date = DateTime.Today;
printDate(date);

// ESERCIZIO #1: Scrivi una lambda che prende due parametri stringa (nome e cognome) e restituisce la loro concatenazione
Func<string, string, string> concatFirstAndLastName = (firstName, lastName) => {
    return firstName + " " + lastName;
};

string firstName = "Vincenzo";
string lastName = "Concilio";
string resultName = concatFirstAndLastName(firstName, lastName);
Console.WriteLine(resultName);

// ESERCIZIO #2: Una lambda che prende tre parametri interi (tre numeri) e restituisce il maggiore dei tre
Func<double, double, double, double> getMaximum = (num1, num2, num3) => {
    double tmp = Math.Max(num1, num2);
    if (tmp > num3)
        return tmp;
    else
        return num3;
};
double num1 = 5.5;
double num2 = 2;
double num3 = 3;
double resultMax = getMaximum(num1, num2, num3);
Console.WriteLine($"Il massimo è {resultMax}");

// ESERCIZIO #3: Una lambda che prende due parametri DateTime e non restituisce nulla, ma stampa la minore delle due date in console con un Console.WriteLine
Action<DateTime, DateTime> printLowerDate = (date1, date2) => {
    int result = DateTime.Compare(date1, date2);
    if (result <= 0)
        Console.WriteLine(date1);
    else
        Console.WriteLine(date2);
};

DateTime date1 = new DateTime(2009, 8, 1, 12, 0, 0);
DateTime date2 = new DateTime(2009, 8, 1, 12, 0, 0);

printLowerDate(date1, date2);