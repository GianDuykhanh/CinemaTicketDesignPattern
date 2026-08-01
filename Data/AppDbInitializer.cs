using Microsoft.EntityFrameworkCore;
using movieCinema.Models;
using MovieCinema.Data.Enums;
using MovieCinema.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using movieCinema.Data.Static;

namespace MovieCinema.Data
{
    public class AppDbInitializer
    {
        public static void Seed(IApplicationBuilder applicationBuilder)
        {
            using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
            {
                var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

                context.Database.EnsureCreated();

                // Cinema
                if (!context.Cinemas.Any())
                {
                    context.Cinemas.AddRange(new List<Cinema>()
                    {
                        new Cinema()
                        {
                            Name = "Fox Cinema",
                            Logo = "/images/f6c8ea25-1fb1-4085-9abf-370d8061df9b_foxcinema.png",
                            Description = "This is the description of the first cinema"
                        },
                        new Cinema()
                        {
                            Name = "AnimalMovies",
                            Logo = "/images/64b82aff-e4c4-4d49-81be-fb2da7957865_animacinema.png",
                            Description = "This is the description of the second cinema"
                        },
                        new Cinema()
                        {
                            Name = "HorrorCinema",
                            Logo = "/images/a8a684b5-28f9-4c90-93b7-1418d762433d_horrorfilms.png",
                            Description = "This is the description of the third cinema"
                        },
                        new Cinema()
                        {
                            Name = "Media Cinema",
                            Logo = "/images/d4eb45a1-81ef-4aa7-81e8-2f9112b55e20_mediacinema.png",
                            Description = "This is the description of the fourth cinema"
                        },
                        new Cinema()
                        {
                            Name = "Cinema Trix",
                            Logo = "/images/5ddc133e-9a06-44cb-9b9c-97a523218679_cinematrix.png",
                            Description = "This is the description of the fifth cinema"
                        }
                    });
                    context.SaveChanges();
                }

                // Cinema Rooms
                if (!context.CinemaRooms.Any())
                {
                    context.CinemaRooms.AddRange(new List<CinemaRoom>()
                    {
                        new CinemaRoom() { Name = "Room A (IMAX)", Capacity = 150, CinemaId = 1 },
                        new CinemaRoom() { Name = "Room B (Standard)", Capacity = 100, CinemaId = 1 },
                        new CinemaRoom() { Name = "Room C (Gold Class)", Capacity = 30, CinemaId = 1 },
                        new CinemaRoom() { Name = "Screen 1", Capacity = 80, CinemaId = 2 },
                        new CinemaRoom() { Name = "Screen 2", Capacity = 120, CinemaId = 2 },
                        new CinemaRoom() { Name = "Hall A", Capacity = 200, CinemaId = 3 },
                        new CinemaRoom() { Name = "Hall B", Capacity = 150, CinemaId = 3 },
                        new CinemaRoom() { Name = "IMAX Theatre", Capacity = 250, CinemaId = 4 },
                        new CinemaRoom() { Name = "Premium Lounge", Capacity = 40, CinemaId = 5 }
                    });
                    context.SaveChanges();
                }

                // Seats
                {
                    var rooms = context.CinemaRooms.ToList();
                    foreach (var room in rooms)
                    {
                        if (!context.Seats.Any(s => s.CinemaRoomId == room.Id))
                        {
                            var seats = new List<Seat>();
                            int seatsPerRow = 10;
                            if (room.Capacity > 120) seatsPerRow = 15;
                            else if (room.Capacity <= 40) seatsPerRow = 8;

                            int rowsCount = (int)Math.Ceiling((double)room.Capacity / seatsPerRow);
                            int seatNum = 1;
                            for (int r = 0; r < rowsCount; r++)
                            {
                                char rowChar = (char)('A' + r);
                                for (int s = 1; s <= seatsPerRow; s++)
                                {
                                    if (seatNum > room.Capacity) break;

                                    var seatType = SeatType.Standard;
                                    if (r == rowsCount - 1)
                                    {
                                        seatType = SeatType.Couple;
                                    }
                                    else if (r >= rowsCount - 3 && r >= 1)
                                    {
                                        seatType = SeatType.VIP;
                                    }
                                    else if (r == 0 && (s == 1 || s == seatsPerRow))
                                    {
                                        seatType = SeatType.Disabled;
                                    }

                                    seats.Add(new Seat()
                                    {
                                        Row = rowChar.ToString(),
                                        Number = s,
                                        SeatType = seatType,
                                        IsAvailable = true,
                                        CinemaRoomId = room.Id
                                    });
                                    seatNum++;
                                }
                            }
                            context.Seats.AddRange(seats);
                            context.SaveChanges();
                        }
                    }
                }




                // Actors
                if (!context.Actors.Any())
                {
                    context.Actors.AddRange(new List<Actor>()
                    {
                        new Actor()
                        {
                            FullName = "Xiao Yan",
                            Bio = "This is the Bio of the first actor",
                            ProfilePictureURL = "/images/05317574-e745-4586-a7c3-2ba09319b4f3_Xiao-Yan.png"
                        },
                        new Actor()
                        {
                            FullName = "Meng Chuan",
                            Bio = "This is the Bio of the second actor",
                            ProfilePictureURL = "/images/623e0876-9216-4dc8-85c5-cecae3b98035_MengChuan.png"
                        },
                        new Actor()
                        {
                            FullName = "Shi Hao",
                            Bio = "This is the Bio of the third actor",
                            ProfilePictureURL = "/images/95f63451-2ae4-44ac-ba1f-c41372409f22_Shi Hao.png"
                        },
                        new Actor()
                        {
                            FullName = "Chu Yuechan",
                            Bio = "This is the Bio of the fourth actor",
                            ProfilePictureURL = "/images/161cf951-c38a-44a9-8cf5-ae838be85c92_Chu Yuechan.png"
                        },
                        new Actor()
                        {
                            FullName = "Lu Xueqi",
                            Bio = "This is the Bio of the fifth actor",
                            ProfilePictureURL = "/images/9e9a80a4-6d80-4c10-85ee-c2f8700cb061_Lu Xueqi.png"
                        }
                    });
                    context.SaveChanges();
                }

                // Producers
                if(!context.Producers.Any())
                {
                    context.Producers.AddRange(new List<Producer>()
                    {
                        new Producer()
                        {
                            FullName = "天蚕土豆",
                            Bio = "This is the Bio of the first actor",
                            ProfilePictureURL = "/images/7cc78953-398f-4dd4-8c5f-ae52db138405_天蚕土豆.png"
                        },
                        new Producer()
                        {
                            FullName = "王林",
                            Bio = "This is the Bio of the second actor",
                            ProfilePictureURL = "/images/193f14a6-050c-4fb2-9e5b-0b55b258d8da_王林.png"
                        },
                        new Producer()
                        {
                            FullName = "Wang Lin",
                            Bio = "This is the Bio of the second actor",
                            ProfilePictureURL = "/images/193f14a6-050c-4fb2-9e5b-0b55b258d8da_王林.png"
                        },
                        new Producer()
                        {
                            FullName = "龙皓晨",
                            Bio = "This is the Bio of the second actor",
                            ProfilePictureURL = "/images/default-avatar.png"
                        },
                        new Producer()
                        {
                            FullName = "张小凡",
                            Bio = "This is the Bio of the second actor",
                            ProfilePictureURL = "/images/default-avatar.png"
                        }
                    });
                    context.SaveChanges();
                }

                // Categories
                if (!context.Categories.Any())
                {
                    context.Categories.AddRange(new List<Category>()
                    {
                        new Category() { Name = "Action", Description = "Action movies filled with suspense and excitement" },
                        new Category() { Name = "Comedy", Description = "Comedy movies that make you laugh" },
                        new Category() { Name = "Drama", Description = "Drama movies with deep storyline and characters" },
                        new Category() { Name = "Documentary", Description = "Informative and educational documentaries" },
                        new Category() { Name = "Horror", Description = "Scary and thrilling horror movies" },
                        new Category() { Name = "Cartoon", Description = "Cartoon and animation movies for all ages" }
                    });
                    context.SaveChanges();
                }

                // Movies
                if (!context.Movies.Any())
                {
                    var categories = context.Categories.ToList();
                    var actionCat = categories.FirstOrDefault(c => c.Name == "Action")?.Id ?? 1;
                    var comedyCat = categories.FirstOrDefault(c => c.Name == "Comedy")?.Id ?? 2;
                    var dramaCat = categories.FirstOrDefault(c => c.Name == "Drama")?.Id ?? 3;
                    var docCat = categories.FirstOrDefault(c => c.Name == "Documentary")?.Id ?? 4;
                    var horrorCat = categories.FirstOrDefault(c => c.Name == "Horror")?.Id ?? 5;
                    var cartoonCat = categories.FirstOrDefault(c => c.Name == "Cartoon")?.Id ?? 6;

                    context.Movies.AddRange(new List<Movie>()
                    {
                        new Movie()
                        {
                            Name = "Dau Pha Thuong Khung",
                            Description = "This is the Dau Pha Thuong Khung movie description",
                            Price = 39.50,
                            ImageURL = "/images/c2980859-7054-44ee-9792-2721a7788091_dptk.png",
                            TrailerURL = "https://youtu.be/3-SCsF0iwik?si=pq7wujresUs7JauK",
                            Duration = 120,
                            StartDate = DateTime.Now.AddDays(-10),
                            EndDate = DateTime.Now.AddDays(10),
                            CinemaId = 3,
                            ProducerId = 3,
                            CategoryId = docCat
                        },
                        new Movie()
                        {
                            Name = "Thuong Nguyen Do",
                            Description = "This is the Thuong Nguyen Do movie description",
                            Price = 29.50,
                            ImageURL = "/images/a6d7cb39-abfa-4a37-95ac-e6f2c7f49494_thuongnguyendo.png",
                            TrailerURL = "https://youtu.be/uRwfXJIXQoQ?si=ZL1HLBV8yzEwVdFY",
                            Duration = 130,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now.AddDays(3),
                            CinemaId = 1,
                            ProducerId = 1,
                            CategoryId = actionCat
                        },
                        new Movie()
                        {
                            Name = "The World Perfect",
                            Description = "This is the The World Perfect movie description",
                            Price = 39.50,
                            ImageURL = "/images/012085fa-4a08-4d4e-b4fd-f74a79930d27_perfect.png",
                            TrailerURL = "https://youtu.be/McNTHeSmSXw?si=Po6u-Iqbw6ECjhFU",
                            Duration = 110,
                            StartDate = DateTime.Now,
                            EndDate = DateTime.Now.AddDays(7),
                            CinemaId = 4,
                            ProducerId = 4,
                            CategoryId = horrorCat
                        },
                        new Movie()
                        {
                            Name = "Nghich Thien Ta Than",
                            Description = "This is the Nghich Thien Ta Than movie description",
                            Price = 39.50,
                            ImageURL = "/images/f9acbbf4-42f8-484b-979f-e9f1940f5364_nttt.png",
                            TrailerURL = "https://youtu.be/Cy9UmJPO1F4?si=JQAYC3_O1A0e650H",
                            Duration = 120,
                            StartDate = DateTime.Now.AddDays(-10),
                            EndDate = DateTime.Now.AddDays(-5),
                            CinemaId = 1,
                            ProducerId = 2,
                            CategoryId = docCat
                        },
                        new Movie()
                        {
                            Name = "Than Mo",
                            Description = "This is the Than Mo movie description",
                            Price = 39.50,
                            ImageURL = "/images/c106c362-0e79-4080-9d58-c29d66595ad2_thanmo.png",
                            TrailerURL = "https://youtu.be/Tc3RT5JI3JI?si=AyoqH6WUT--fTVGM",
                            Duration = 100,
                            StartDate = DateTime.Now.AddDays(-10),
                            EndDate = DateTime.Now.AddDays(-2),
                            CinemaId = 1,
                            ProducerId = 3,
                            CategoryId = cartoonCat
                        },
                        new Movie()
                        {
                            Name = "Su Huynh A Su Huynh",
                            Description = "This is the Su Huynh A Su Huynh movie description",
                            Price = 39.50,
                            ImageURL = "/images/b62771a3-6689-4bdf-b286-38742a7118f2_suhuyn.png",
                            TrailerURL = "https://youtu.be/k36XJ5yFb68?si=YMF6f8cvSOtmOf3h",
                            Duration = 90,
                            StartDate = DateTime.Now.AddDays(3),
                            EndDate = DateTime.Now.AddDays(20),
                            CinemaId = 1,
                            ProducerId = 5,
                            CategoryId = dramaCat
                        }
                    });
                    context.SaveChanges();
                }

                // Actors & Movies
                if (!context.Actors_Movies.Any())
                {
                    context.Actors_Movies.AddRange(new List<Actor_Movie>()
                    {
                        new Actor_Movie()
                        {
                            ActorId = 1,
                            MovieId = 1
                        },
                        new Actor_Movie()
                        {
                            ActorId = 3,
                            MovieId = 1
                        },

                         new Actor_Movie()
                        {
                            ActorId = 1,
                            MovieId = 2
                        },
                         new Actor_Movie()
                        {
                            ActorId = 4,
                            MovieId = 2
                        },

                        new Actor_Movie()
                        {
                            ActorId = 1,
                            MovieId = 3
                        },
                        new Actor_Movie()
                        {
                            ActorId = 2,
                            MovieId = 3
                        },
                        new Actor_Movie()
                        {
                            ActorId = 5,
                            MovieId = 3
                        },


                        new Actor_Movie()
                        {
                            ActorId = 2,
                            MovieId = 4
                        },
                        new Actor_Movie()
                        {
                            ActorId = 3,
                            MovieId = 4
                        },
                        new Actor_Movie()
                        {
                            ActorId = 4,
                            MovieId = 4
                        },


                        new Actor_Movie()
                        {
                            ActorId = 2,
                            MovieId = 5
                        },
                        new Actor_Movie()
                        {
                            ActorId = 3,
                            MovieId = 5
                        },
                        new Actor_Movie()
                        {
                            ActorId = 4,
                            MovieId = 5
                        },
                        new Actor_Movie()
                        {
                            ActorId = 5,
                            MovieId = 5
                        },


                        new Actor_Movie()
                        {
                            ActorId = 3,
                            MovieId = 6
                        },
                        new Actor_Movie()
                        {
                            ActorId = 4,
                            MovieId = 6
                        },
                        new Actor_Movie()
                        {
                            ActorId = 5,
                            MovieId = 6
                        },
                    });
                    context.SaveChanges();
                }

                // Showtimes
                if (!context.Showtimes.Any())
                {
                    var movies = context.Movies.ToList();
                    var rooms = context.CinemaRooms.ToList();

                    if (movies.Any() && rooms.Any())
                    {
                        var showtimes = new List<Showtime>();

                        // Showtime for movie 1
                        var m1 = movies.FirstOrDefault(m => m.Name == "Dau Pha Thuong Khung");
                        var r1 = rooms.FirstOrDefault(r => r.Name == "Room A (IMAX)") ?? rooms[0];
                        var r2 = rooms.FirstOrDefault(r => r.Name == "Room B (Standard)") ?? rooms[0];
                        if (m1 != null)
                        {
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddHours(10),
                                EndTime = DateTime.Today.AddHours(12),
                                Price = 45000,
                                MovieId = m1.Id,
                                CinemaRoomId = r1.Id
                            });
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddHours(14),
                                EndTime = DateTime.Today.AddHours(16),
                                Price = 45000,
                                MovieId = m1.Id,
                                CinemaRoomId = r1.Id
                            });
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddDays(1).AddHours(18),
                                EndTime = DateTime.Today.AddDays(1).AddHours(20),
                                Price = 50000,
                                MovieId = m1.Id,
                                CinemaRoomId = r2.Id
                            });
                        }

                        // Showtime for movie 2
                        var m2 = movies.FirstOrDefault(m => m.Name == "Thuong Nguyen Do");
                        var r3 = rooms.FirstOrDefault(r => r.Name == "Room C (Gold Class)") ?? rooms[0];
                        var r4 = rooms.FirstOrDefault(r => r.Name == "Screen 1") ?? rooms[0];
                        if (m2 != null)
                        {
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddHours(11).AddMinutes(30),
                                EndTime = DateTime.Today.AddHours(13).AddMinutes(40),
                                Price = 40000,
                                MovieId = m2.Id,
                                CinemaRoomId = r3.Id
                            });
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddHours(16),
                                EndTime = DateTime.Today.AddHours(18).AddMinutes(10),
                                Price = 40000,
                                MovieId = m2.Id,
                                CinemaRoomId = r4.Id
                            });
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddDays(1).AddHours(20).AddMinutes(30),
                                EndTime = DateTime.Today.AddDays(1).AddHours(22).AddMinutes(40),
                                Price = 45000,
                                MovieId = m2.Id,
                                CinemaRoomId = r3.Id
                            });
                        }

                        // Showtime for movie 3
                        var m3 = movies.FirstOrDefault(m => m.Name == "The World Perfect");
                        var r5 = rooms.FirstOrDefault(r => r.Name == "Screen 2") ?? rooms[0];
                        if (m3 != null)
                        {
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddHours(9),
                                EndTime = DateTime.Today.AddHours(10).AddMinutes(50),
                                Price = 45000,
                                MovieId = m3.Id,
                                CinemaRoomId = r5.Id
                            });
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddDays(1).AddHours(15),
                                EndTime = DateTime.Today.AddDays(1).AddHours(16).AddMinutes(50),
                                Price = 45000,
                                MovieId = m3.Id,
                                CinemaRoomId = r5.Id
                            });
                        }

                        // Showtime for movie 4
                        var m4 = movies.FirstOrDefault(m => m.Name == "Nghich Thien Ta Than");
                        var r6 = rooms.FirstOrDefault(r => r.Name == "Hall A") ?? rooms[0];
                        if (m4 != null)
                        {
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddHours(13),
                                EndTime = DateTime.Today.AddHours(15),
                                Price = 35000,
                                MovieId = m4.Id,
                                CinemaRoomId = r6.Id
                            });
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddDays(1).AddHours(17).AddMinutes(30),
                                EndTime = DateTime.Today.AddDays(1).AddHours(19).AddMinutes(30),
                                Price = 38000,
                                MovieId = m4.Id,
                                CinemaRoomId = r6.Id
                            });
                        }

                        // Showtime for movie 5
                        var m5 = movies.FirstOrDefault(m => m.Name == "Than Mo");
                        var r7 = rooms.FirstOrDefault(r => r.Name == "Hall B") ?? rooms[0];
                        if (m5 != null)
                        {
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddHours(14).AddMinutes(30),
                                EndTime = DateTime.Today.AddHours(16).AddMinutes(10),
                                Price = 40000,
                                MovieId = m5.Id,
                                CinemaRoomId = r7.Id
                            });
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddDays(1).AddHours(19),
                                EndTime = DateTime.Today.AddDays(1).AddHours(20).AddMinutes(40),
                                Price = 45000,
                                MovieId = m5.Id,
                                CinemaRoomId = r7.Id
                            });
                        }

                        // Showtime for movie 6
                        var m6 = movies.FirstOrDefault(m => m.Name == "Su Huynh A Su Huynh");
                        var r8 = rooms.FirstOrDefault(r => r.Name == "IMAX Theatre") ?? rooms[0];
                        if (m6 != null)
                        {
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddHours(16).AddMinutes(30),
                                EndTime = DateTime.Today.AddHours(18),
                                Price = 42000,
                                MovieId = m6.Id,
                                CinemaRoomId = r8.Id
                            });
                            showtimes.Add(new Showtime()
                            {
                                StartTime = DateTime.Today.AddDays(1).AddHours(21),
                                EndTime = DateTime.Today.AddDays(1).AddHours(22).AddMinutes(30),
                                Price = 45000,
                                MovieId = m6.Id,
                                CinemaRoomId = r8.Id
                            });
                        }

                        context.Showtimes.AddRange(showtimes);
                        context.SaveChanges();
                    }
                }

                // Movie Reviews Seed
                if (!context.MovieReviews.Any())
                {
                    var movies = context.Movies.ToList();
                    var m1 = movies.FirstOrDefault(m => m.Name == "Dau Pha Thuong Khung");
                    var m2 = movies.FirstOrDefault(m => m.Name == "Thuong Nguyen Do");
                    var m3 = movies.FirstOrDefault(m => m.Name == "The World Perfect");

                    var reviews = new List<MovieReview>();

                    if (m1 != null)
                    {
                        reviews.Add(new MovieReview() { Name = "Gia Lâm", Email = "gialam@gmail.com", Rating = 5, Comment = "Phim quá tuyệt vời! Kỹ xảo 3D đẹp mắt, bám sát nguyên tác đấu khí cực đỉnh. Mong đợi phần tiếp theo!", CreatedAt = DateTime.Now.AddDays(-5), MovieId = m1.Id });
                        reviews.Add(new MovieReview() { Name = "Minh Tuấn", Email = "tuanminh@gmail.com", Rating = 4, Comment = "Cốt truyện hấp dẫn, âm thanh hoành tráng. Nhân vật Tiêu Viêm tạo hình rất ngầu.", CreatedAt = DateTime.Now.AddDays(-4), MovieId = m1.Id });
                        reviews.Add(new MovieReview() { Name = "Hương Giang", Email = "giangh@gmail.com", Rating = 5, Comment = "Được đề xuất xem và thực sự không thất vọng chút nào! 5 sao cho chất lượng hình ảnh.", CreatedAt = DateTime.Now.AddDays(-2), MovieId = m1.Id });
                    }

                    if (m2 != null)
                    {
                        reviews.Add(new MovieReview() { Name = "Khánh Duy", Email = "duykhanh@gmail.com", Rating = 4, Comment = "Nội dung phim khá hay và hồi hộp, các pha hành động nghẹt thở. Phòng chiếu IMAX xem rất đã.", CreatedAt = DateTime.Now.AddDays(-3), MovieId = m2.Id });
                        reviews.Add(new MovieReview() { Name = "Thu Thủy", Email = "thuyt@gmail.com", Rating = 3, Comment = "Phim ổn, nhịp phim hơi nhanh ở đoạn giữa nhưng tổng thể xem giải trí rất tốt.", CreatedAt = DateTime.Now.AddDays(-1), MovieId = m2.Id });
                    }

                    if (m3 != null)
                    {
                        reviews.Add(new MovieReview() { Name = "Hoàng Long", Email = "longh@gmail.com", Rating = 5, Comment = "Phim siêu đỉnh! Thạch Hạo vô địch! Kỹ xảo đánh nhau hoành tráng nhất trong các bộ từng xem.", CreatedAt = DateTime.Now.AddDays(-2), MovieId = m3.Id });
                    }

                    context.MovieReviews.AddRange(reviews);
                    context.SaveChanges();
                }

                // Vouchers Seed
                if (!context.Vouchers.Any())
                {
                    context.Vouchers.AddRange(new List<Voucher>()
                    {
                        new Voucher()
                        {
                            Code = "GIAM50K",
                            DiscountAmount = 50000,
                            DiscountPercentage = 0,
                            IsPercentage = false,
                            MinOrderAmount = 100000,
                            ExpiryDate = DateTime.Now.AddDays(30),
                            IsActive = true
                        },
                        new Voucher()
                        {
                            Code = "GIAM10",
                            DiscountAmount = 0,
                            DiscountPercentage = 10,
                            IsPercentage = true,
                            MinOrderAmount = 50000,
                            ExpiryDate = DateTime.Now.AddDays(30),
                            IsActive = true
                        },
                        new Voucher()
                        {
                            Code = "WELCOME",
                            DiscountAmount = 20000,
                            DiscountPercentage = 0,
                            IsPercentage = false,
                            MinOrderAmount = 0,
                            ExpiryDate = DateTime.Now.AddDays(30),
                            IsActive = true
                        }
                    });
                    context.SaveChanges();
                }

                // Members Seed
                if (!context.Members.Any())
                {
                    context.Members.AddRange(new List<Member>()
                    {
                        new Member()
                        {
                            Email = "duykhanh@gmail.com",
                            Name = "Khánh Duy",
                            Points = 120
                        },
                        new Member()
                        {
                            Email = "gialam@gmail.com",
                            Name = "Gia Lâm",
                            Points = 50
                        }
                    });
                    context.SaveChanges();
                }
            }
        }

        public static async Task SeedUsersAndRolesAsync(IApplicationBuilder applicationBuilder)
        {
            using(var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
            {
                // roles
                var roleManager = serviceScope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                if (!await roleManager.RoleExistsAsync(UserRoles.Admin))
                    await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));

                if (!await roleManager.RoleExistsAsync(UserRoles.User))
                    await roleManager.CreateAsync(new IdentityRole(UserRoles.User));

                // Users
                var userManager = serviceScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                string adminUserEmail = "admin@tickets.com";

                var adminUser = await userManager.FindByEmailAsync(adminUserEmail);
                if(adminUser == null)
                {
                    var newAdminUser = new ApplicationUser()
                    {
                        FullName = "Admin User",
                        UserName = "admin-user",
                        Email = adminUserEmail,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(newAdminUser, "Coding@1234?");
                    await userManager.AddToRoleAsync(newAdminUser, UserRoles.Admin);
                }


                string appUserEmail = "user@tickets.com";

                var appUser = await userManager.FindByEmailAsync(appUserEmail);
                if(appUser == null)
                {
                    var newAppUser = new ApplicationUser()
                    {
                        FullName = "Application User",
                        UserName = "app-user",
                        Email = appUserEmail,
                        EmailConfirmed = true
                    };
                    await userManager.CreateAsync(newAppUser, "Coding@1234?");
                    await userManager.AddToRoleAsync(newAppUser, UserRoles.User);
                }
            }
        }
    }
}
