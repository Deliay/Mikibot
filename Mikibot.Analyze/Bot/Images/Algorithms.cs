namespace Mikibot.Analyze.Bot.Images;

public class Algorithms
{
    public static int Gcd(int a, int b) {
        while (b != 0) {
            var temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }
    public static int Lcm(int a, int b) {
        var gcd = Gcd(a, b);
        return (a * b) / gcd;
    }
}