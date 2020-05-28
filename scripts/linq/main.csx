class Apple {
    public string Color { get; set; }
    public int Weight { get; set; }
}

List<Apple> apples = new List<Apple> {
    new Apple { Color = "Red", Weight = 185 },
    new Apple { Color = "Green", Weight = 195 },
    new Apple { Color = "Red", Weight = 190 },
    new Apple { Color = "Green", Weight = 180 },
};

//Func<Apple, bool> takeRedApples = apple => apple.Color == "Red";
IEnumerable<int> weightsOfRedApples = apples
                                .Where(apple => apple.Color == "Red")
                                .Select(apple => apple.Weight);

double average = weightsOfRedApples.Average();
Console.WriteLine(average);

//qual'è il peso minimo: int minimumWeight = apples...;
int minimumWeight = apples
                .Select(apple => apple.Weight)
                .Min();

Console.WriteLine(minimumWeight);

//colore della mela che pesa 190 grammi: string color = apples...;
IEnumerable<string> color190 = apples
            .Where(apple => apple.Weight == 190)
            .Select(apple => apple.Color);


Console.WriteLine(color190.Single().ToString());

//quante sono le mele rosse: int redAppleCount = apples...;
int redAppleCount = apples
                .Where(apple => apple.Color == "Red")
                .Count();
Console.WriteLine(redAppleCount);

//quanto pesano in totale le mele verdi: int totalWeight = apples...;
int totalWeightGreen = apples
                    .Where(apple => apple.Color == "Green")
                    .Select(apple => apple.Weight)
                    .Sum();
Console.WriteLine(totalWeightGreen);