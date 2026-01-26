namespace ClassLibrary;

public class Person
{
    private string _nom;

    public string Nom
    {
        get => _nom;
        set => _nom = value ?? throw new ArgumentNullException(nameof(value));
    }
}