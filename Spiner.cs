namespace musicDL
{
    public class Spiner
    {
        private static int counter = 0;
        private static readonly char[] chars = { '|', '/', '-', '\\' };

        public static void Spin(string message)
        {
            counter++;
            Console.Write($"\r{message} {chars[counter % 4]}");
        }
    }
}