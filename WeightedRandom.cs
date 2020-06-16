#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;

namespace Com.Mindstyler.Additional
{
    public class WeightedRandom
    {
        private readonly Random random;

        public WeightedRandom()
        {
            random = new Random();
        }

        public WeightedRandom(int seed)
        {
            random = new Random(seed);
        }

        public WeightedRandom(string seed)
        {
            random = new Random(seed.GetHashCode());
        }

        /// <summary>
        /// Get a weighted random element of the list provided. Each element in this list must define it's own probability.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <returns></returns>
        public T GetWeightedRandom<T>(List<KeyValuePair<T, float>> elements)
        {
            float diceRoll = (float)random.NextDouble();
            float cumulative = 0.0f;
            float safetyCheck = 0.0f;

            foreach (KeyValuePair<T, float> element in elements)
            {
                safetyCheck += element.Value;
            }

            if (safetyCheck != 1.0f)
            {
                //TODO make better stuff to return to previous code without crashing
                throw new Exception($"Percentage didn't match up to exactly 1.0. Percentage: {safetyCheck}");
            }

            for (int i = 0; i < elements.Count; ++i)
            {
                cumulative += elements[i].Value;

                if (diceRoll <= cumulative)
                {
                    return elements[i].Key;
                }
            }

            //TODO this is newly implemented, it should never return but if it does make sure it's properly handled
            return default;
        }

        /// <summary>
        /// Get a random item from a weighted chosen list of the provided list. Items have no own probability but each list does.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <returns></returns>
        public T GetWeightedRandom<T>(List<KeyValuePair<List<T>, float>> elements)
        {
            float diceRoll = (float)random.NextDouble();
            float cumulative = 0.0f;
            float safetyCheck = 0.0f;

            foreach (KeyValuePair<List<T>, float> element in elements)
            {
                safetyCheck += element.Value;
            }

            if (safetyCheck != 1.0f)
            {
                //TODO make better stuff to return to previous code without crashing
                throw new Exception($"Percentage didn't match up to exactly 100. Percentage: {safetyCheck}");
            }

            for (int i = 0; i < elements.Count; ++i)
            {
                cumulative += elements[i].Value;

                if (diceRoll <= cumulative)
                {
                    return elements[i].Key[random.Next(0, elements[i].Key.Count)];
                }
            }

            //TODO this is newly implemented, it should never return but if it does make sure it's properly handled (returns default 'empty' value of T)
            return default;
        }
    }
}
