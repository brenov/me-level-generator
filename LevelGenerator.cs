using System;
using System.Collections.Generic;

namespace LevelGenerator
{
    /// This class holds the evolutionary level generation algorithm.
    public class LevelGenerator
    {
        /// The number of parents to be selected for crossover.
        private static readonly int CROSSOVER_PARENTS = 2;

        /// The evolutionary parameters.
        private Parameters prs;
        /// The found MAP-Elites population.
        private Population solution;
        /// The evolutionary process' collected data.
        private Data data;

        /// Return the found MAP-Elites population.
        public Population Solution { get => solution; }

        /// Return the collected data from the evolutionary process.
        public Data Data { get => data; }

        /// Level Generator constructor.
        public LevelGenerator(
            Parameters _prs
        ) {
            prs = _prs;
            data = new Data();
            data.parameters = prs;
        }

        /// Generate and return a set of levels.
        public Population Evolve()
        {
            DateTime start = DateTime.Now;
            Evolution();
            DateTime end = DateTime.Now;
            data.duration = (end - start).TotalSeconds;
            return solution;
        }

        /// Perform the level evolution process.
        private void Evolution()
        {
            // Initialize the random generator
            Random rand = new Random(prs.seed);

            // Initialize the MAP-Elites population
            Population pop = new Population(
                SearchSpace.CoefficientOfExplorationRanges().Length,
                SearchSpace.LeniencyRanges().Length
            );

            // Generate the initial population
            while (pop.Count() < prs.population)
            {
                Individual individual = Individual.GetRandom(
                    prs.enemies, ref rand
                );
                individual.dungeon.Fix(prs, ref rand);
                individual.CalculateLinearCoefficient();
                Fitness.Calculate(prs, ref individual, ref rand);
                float ce = Metric.CoefficientOfExploration(individual);
                float le = Metric.Leniency(individual);
                individual.exploration = ce;
                individual.leniency = le;
                pop.PlaceIndividual(individual);
            }

            // Evolve the population
            int g = 0;
            DateTime start = DateTime.Now;
            DateTime end = DateTime.Now;
            while ((end - start).TotalSeconds < prs.time)
            {
                List<Individual> intermediate = new List<Individual>();
                while (intermediate.Count < prs.intermediate)
                {
                    // Apply the crossover operation
                    Individual[] parents = Selection.Select(
                        CROSSOVER_PARENTS, prs.competitors, pop, ref rand
                    );
                    Individual[] offspring = Crossover.Apply(
                        parents[0], parents[1], ref rand
                    );
                    // Apply the mutation operation with a random chance or
                    // always that the crossover was not successful
                    if (offspring.Length == 0 ||
                        prs.mutation > Common.RandomPercent(ref rand)
                    ) {
                        if (offspring.Length == CROSSOVER_PARENTS)
                        {
                            parents[0] = offspring[0];
                            parents[1] = offspring[1];
                        }
                        else
                        {
                            offspring = new Individual[2];
                        }
                        offspring[0] = Mutation.Apply(parents[0], ref rand);
                        offspring[1] = Mutation.Apply(parents[1], ref rand);
                    }
                    // Place the offspring in the intermediate population
                    for (int i = 0; i < offspring.Length; i++)
                    {
                        offspring[i].dungeon.Fix(prs, ref rand);
                        offspring[i].CalculateLinearCoefficient();
                        Fitness.Calculate(prs, ref offspring[i], ref rand);
                        float c = Metric.CoefficientOfExploration(offspring[i]);
                        float l = Metric.Leniency(offspring[i]);
                        offspring[i].exploration = c;
                        offspring[i].leniency = l;
                        offspring[i].generation = g;
                        intermediate.Add(offspring[i]);
                    }
                }

                // Place the intermediate population in the MAP-Elites
                foreach (Individual individual in intermediate)
                {
                    pop.PlaceIndividual(individual);
                }

                g++;
                end = DateTime.Now;
            }

            data.generations = g;

            // Get the final population (solution)
            solution = pop;
        }
    }
}