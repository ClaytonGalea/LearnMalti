namespace LearnMalti.Data
{
    public static class SeedImageUrls
    {
        public static void Seed(AppDbContext db)
        {
            var items = db.LearningItems.ToList();

            foreach (var item in items)
            {
                if (item.EnglishText == "Apple")
                    item.ImageUrl = "/images/words/apple.png";

                if (item.EnglishText == "Water")
                    item.ImageUrl = "/images/words/water.png";

                if (item.EnglishText == "Dog")
                    item.ImageUrl = "/images/words/dog.png";

                if (item.EnglishText == "Shop")
                    item.ImageUrl = "/images/words/shop.png";

                if (item.EnglishText == "Friend")
                    item.ImageUrl = "/images/words/friend.png";

                if (item.EnglishText == "Books")
                    item.ImageUrl = "/images/words/books.png";

                if (item.EnglishText == "House")
                    item.ImageUrl = "/images/words/house.png";

                if (item.EnglishText == "School")
                    item.ImageUrl = "/images/words/school.png";

                if (item.EnglishText == "Chair")
                    item.ImageUrl = "/images/words/chair.png";
            }

            db.SaveChanges();
        }
    }
}
