using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace Submitter
{
    class SolutionResponseEntity
    {
        public bool ok { get; set; }
        public double resemblance { get; set; }
    }


    class SnapshotEntity
    {
        public List<SnapshotProblemEntity> problems;
    }

    class SnapshotProblemEntity
    {
        public int problem_id { get; set; }
        public string problem_spec_hash { get; set;  }
    }

    class DbEntity
    {
        public List<DbProblemEntity> problems { get; set; }
    }

    class DbProblemEntity
    {
        public int id { get; set; }
        public string problem_spec_hash { get; set; }
        public double mySimilarity { get; set; }
        public double serverSimilarity { get; set; }
        public string lastServerResult { get; set; }
    }

    class Program
    {
        static int TaskCount = 1;
        static object syncRoot = new object();
        static DbEntity Db;
        static SnapshotEntity Snapshot;
        static IEnumerator<string> Problems;
        static DateTime LastSubmitTime;

        static void Main(string[] args)
        {
            Db = JsonConvert.DeserializeObject<DbEntity>(File.ReadAllText("/icfp2016/work/db.json"));
            Snapshot = JsonConvert.DeserializeObject<SnapshotEntity>(File.ReadAllText("/icfp2016/work/current.json"));
            LastSubmitTime = DateTime.MinValue;
            // var problems = File.ReadAllLines(args[0]);
            var problems = new List<string>();
            for (var i = 1; i < 100; ++i)
            {
                problems.Add(i.ToString());
            }
            
            Problems = problems.AsEnumerable<string>().GetEnumerator();
            TaskCount = 1;
            foreach (var problem in problems)
            {
                DispatchOne();
            }
            CompleteOne();
            while (true);
        }

        static void DispatchOne()
        {
            SnapshotProblemEntity problem = null;

            while (problem == null)
            {
                lock (syncRoot)
                {
                    if (TaskCount == 20)
                    {
                        return;
                    }

                    if (Problems == null || Problems.MoveNext() == false)
                    {
                        if (TaskCount == 0)
                        {
                            Environment.Exit(0);
                        }
                        Problems = null;
                        return;
                    }

                    int problemId = int.Parse(Problems.Current);
                    problem = Snapshot.problems.FirstOrDefault(i => i.problem_id == problemId);
                }

                ++TaskCount;
            }

            var task = SolveOne(problem);
        }

        static async Task SolveOne(SnapshotProblemEntity snapshotProblem)
        {
            try
            {
                Console.Out.WriteLine("Solving #{0} ({1})", snapshotProblem.problem_id, snapshotProblem.problem_spec_hash.Substring(0, 9));
                var ans = await SolveOneImpl(snapshotProblem);
                Console.Out.WriteLine("Solved  #{0} ({1}): {2} {3}", 
                    snapshotProblem.problem_id,
                    snapshotProblem.problem_spec_hash.Substring(0, 9),
                    ans.mySimilarity,
                    ans.serverSimilarity);
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("Error #{0} ({1}): {2}", snapshotProblem.problem_id, snapshotProblem.problem_spec_hash, e.ToString());
            }

            CompleteOne();
        }

        static async Task<DbProblemEntity> SolveOneImpl(SnapshotProblemEntity snapshotProblem)
        {
            lock (syncRoot)
            {
                var problem = Db.problems.FirstOrDefault(i => i.id == snapshotProblem.problem_id);
                if (problem != null && problem.serverSimilarity == 1.0)
                {
                    // I've already solved this one.
                    return problem;
                }
            }

            var ans = await ExecProcessAsync(SOLVER, snapshotProblem.problem_spec_hash);
            var mySimilarity = double.Parse(ans);
            DateTime submitTime;

            lock (syncRoot)
            {
                var problem = Db.problems.FirstOrDefault(i => i.id == snapshotProblem.problem_id);
                if (problem == null)
                {
                    problem = new DbProblemEntity()
                    {
                        id = snapshotProblem.problem_id,
                        problem_spec_hash = snapshotProblem.problem_spec_hash
                    };
                    Db.problems.Add(problem);
                }

                if (mySimilarity <= problem.serverSimilarity)
                {
                    // My answer is no better, end now.
                    return problem;
                }

                problem.mySimilarity = mySimilarity;

                TimeSpan minTimeSpan = TimeSpan.FromMilliseconds(1500);
                submitTime = DateTime.UtcNow;
                if (submitTime < LastSubmitTime + minTimeSpan)
                {
                    submitTime = LastSubmitTime + minTimeSpan;
                }
                LastSubmitTime = submitTime;
            }

            await Task.Delay(submitTime - DateTime.UtcNow);

            var serverResult = await ExecProcessAsync(
                CURL,
                string.Format("--compressed -L -H Expect: -H 'X-API-Key: 61-d481c90ded9129b1fa54f905fa3d4eeb' -F 'problem_id={0}' -F 'solution_spec=@d:\\icfp2016\\work\\solutions\\{1}' 'http://2016sv.icfpcontest.org/api/solution/submit'",
                    snapshotProblem.problem_id,
                    snapshotProblem.problem_spec_hash));
            //var serverResult = "{ \"ok\": true, \"resemblance\": 1.0 }";
            var serverResultEntity = JsonConvert.DeserializeObject<SolutionResponseEntity>(serverResult);

            lock (syncRoot)
            {
                var problem = Db.problems.Single(i => i.id == snapshotProblem.problem_id);
                problem.lastServerResult = serverResult;
                if (serverResultEntity != null && serverResultEntity.ok)
                {
                    problem.serverSimilarity = serverResultEntity.resemblance;
                }
                File.Delete("/icfp2016/work/db.bak.json");
                File.Move("/icfp2016/work/db.json", "/icfp2016/work/db.bak.json");
                File.WriteAllText("/icfp2016/work/db.json", JsonConvert.SerializeObject(Db, Formatting.Indented));

                return problem;
            }
        }

        static void CompleteOne()
        {
            lock (syncRoot)
            {
                --TaskCount;
            }

            DispatchOne();
        }

        static Task<string> ExecProcessAsync(string exeName, string arguments)
        {
            var process = new Process();
            process.StartInfo = new ProcessStartInfo()
            {
                UseShellExecute = false,
                FileName = exeName,
                CreateNoWindow = true,
                Arguments = arguments
            };
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;

            var tcs = new TaskCompletionSource<string>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => {
                tcs.TrySetResult(process.StandardOutput.ReadToEnd());
            };
            process.Start();

            return tcs.Task;
        }

        private static string CURL = @"C:\Users\cashto\AppData\Local\GitHub\PortableGit_d76a6a98c9315931ec4927243517bc09e9b731a0\usr\bin\curl.exe";
        private static string SOLVER = @"D:\icfp2016\Solver\bin\Debug\Solver.exe";
    }
}
