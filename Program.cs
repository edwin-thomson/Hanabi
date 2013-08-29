using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hanabi
{
    class Program
    {
        const int NumGames = 1;
        static void Main(string[] args)
        {
            int[] scores = new int[26];
            int total = 0;
            var seeder = new Random(); // pretty bad...
            for (int i = 0; i < NumGames; i++)
            {
                Game g = new Game(seeder.Next(), NumGames == 1);
                for (int j = 0; j < 4; j++)
                    g.AddPlayer(new BasicPlayer());
                int score = g.Play();
                scores[score]++;
                total += score;
            }
            Console.Write("Scores: ");
            for (int i = 0; i < 26; i++)
                Console.Write("{0}:{1} ", i, scores[i]);
            Console.WriteLine();
            Console.WriteLine("Total: {0} Avg: {1}", total, (float)total / NumGames);
            Console.ReadLine();
        }
    }
}
