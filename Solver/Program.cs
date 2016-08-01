using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    public class Program
    {
        static void Main(string[] args)
        {
        }

        public static bool isPrime(int x)
        {
            for (int i = 2; i * i <= x; ++i)
            {
                if (x % i == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
