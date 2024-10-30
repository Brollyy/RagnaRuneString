using RagnaRuneString;

namespace RagnaRuneStringTest.Version1
{
    [TestClass]
    public class DeserializeVersion1UnitTest
    {
        [TestMethod]
        public void DeserializeEmpty()
        {
            string runeString = Convert.ToBase64String([
                0x00, 0x01,     // header
                0x00,           // single rune count 
                0x00,           // double rune count
                0x00,           // n-rune count
                0x00            // BPM changes count
            ]);

            RagnaRuneString.Version1.RuneStringData expectedRuneStringData = new([], []);
            Assert.AreEqual(expectedRuneStringData, RuneStringSerializer.DeserializeV1(runeString));
        }

        [TestMethod]
        public void DeserializeOnlySingleRunes()
        {
            string runeString = Convert.ToBase64String([
                0x00, 0x01,     // header
                0x04,           // single rune count 
                0xC4, 0x13,     // varint 2500 (0.25 * 10000)  -> 00000000 00000000 00001001 11000100 -> 11000100 00010011
                0x88, 0x27,     // varint 5000 (0.5 * 10000)   -> 00000000 00000000 00010011 10001000 -> 10001000 00100111
                0xCC, 0x3A,     // varint 7500 (0.75 * 10000)  -> 00000000 00000000 00011101 01001100 -> 11001100 00111010
                0x90, 0x4E,     // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x1B,           // single rune column byte (00 01 10 11)
                0x00,           // double rune count
                0x00,           // n-rune count
                0x00            // BPM changes count
            ]);

            RagnaRuneString.Version1.RuneStringData expectedRuneStringData = new(
                [
                    new(0.25, 0),
                    new(0.5, 1),
                    new(0.75, 2),
                    new(1, 3)
                ],
                []
             );
            Assert.AreEqual(expectedRuneStringData, RuneStringSerializer.DeserializeV1(runeString));
        }

        [TestMethod]
        public void DeserializeOnlyDoubleRunes()
        {
            string runeString = Convert.ToBase64String([
                0x00, 0x01,         // header
                0x00,               // single rune count
                0x04,               // double rune count
                0xC4, 0x13,         // varint 2500 (0.25 * 10000)  -> 00000000 00000000 00001001 11000100 -> 11000100 00010011
                0x88, 0x27,         // varint 5000 (0.5 * 10000)   -> 00000000 00000000 00010011 10001000 -> 10001000 00100111
                0xCC, 0x3A,         // varint 7500 (0.75 * 10000)  -> 00000000 00000000 00011101 01001100 -> 11001100 00111010
                0x90, 0x4E,         // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x4C, 0xC0, 0x00,   // double rune column combo bytes (010 011 001 100), with additional 12 bits of padding.
                0x00,               // n-rune count
                0x00                // BPM changes count
            ]);

            RagnaRuneString.Version1.RuneStringData expectedRuneStringData = new(
                [
                    new(0.25, 0), new(0.25, 3),
                    new(0.5, 1), new (0.5, 2),
                    new(0.75, 0), new(0.75, 2),
                    new(1, 1), new(1, 3)
                ],
                []
            );
            Assert.AreEqual(expectedRuneStringData, RuneStringSerializer.DeserializeV1(runeString));
        }

        [TestMethod]
        public void DeserializeOnlyNRunes()
        {
            string runeString = Convert.ToBase64String([
                0x00, 0x01,                 // header
                0x00,                       // single rune count
                0x00,                       // double rune count
                0x02,                       // n-rune count
                0x90, 0x4E,                 // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x03,                       // n for first n-rune
                0x00, 0x01, 0x02,           // column bytes (0, 1, 2)
                0xA0, 0x9C, 0x01,           // varint 20000 (2 * 20000)    -> 00000000 00000000 01001110 00100000 -> 10100000 10011100 00000001
                0x04,                       // n for second n-rune
                0x00, 0x01, 0x02, 0x03,     // column bytes (0, 1, 2, 3)
                0x00                        // BPM changes count
            ]);

            RagnaRuneString.Version1.RuneStringData expectedRuneStringData = new(
                [
                    new(1, 0), new(1, 1), new(1, 2),
                    new(2, 0), new(2, 1), new(2, 2), new(2, 3)
                ],
                []
            );
            Assert.AreEqual(expectedRuneStringData, RuneStringSerializer.DeserializeV1(runeString));
        }

        [TestMethod]
        public void DeserializeRunesWithRoundingAndPadding()
        {
            string runeString = Convert.ToBase64String([
                0x00, 0x01,     // header
                0x05,           // single rune count
                0xC4, 0x13,     // varint 2500 (0.25 * 10000)  -> 00000000 00000000 00001001 11000100 -> 11000100 00010011
                0x85, 0x1A,     // varint 3333 (0.(3) * 10000) -> 00000000 00000000 00001100 10000101 -> 10000101 00011010
                0x88, 0x27,     // varint 5000 (0.5 * 10000)   -> 00000000 00000000 00010011 10001000 -> 10001000 00100111
                0xCC, 0x3A,     // varint 7500 (0.75 * 10000)  -> 00000000 00000000 00011101 01001100 -> 11001100 00111010
                0x90, 0x4E,     // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x36, 0xC0,     // single rune column bytes (00 11 01 10 11), with additional 6 bits of zero padding
                0x00,           // double rune count
                0x00,           // n-rune count
                0x00            // BPM changes count
            ]);

            RagnaRuneString.Version1.RuneStringData expectedRuneStringData = new(
                [
                    new(0.25, 0),
                    new(1.0 / 3, 3),
                    new(0.5, 1),
                    new(0.75, 2),
                    new(1, 3)
                ],
                []
            );
            Assert.AreEqual(expectedRuneStringData, RuneStringSerializer.DeserializeV1(runeString));
        }

        [TestMethod]
        public void DeserializeOnlyBPMChanges()
        {
            string runeString = Convert.ToBase64String([
                0x00, 0x01,                 // header
                0x00,                       // single rune count
                0x00,                       // double rune count
                0x00,                       // n-rune count
                0x03,                       // BPM changes count
                0x00,                       // time - varint 0 (0 * 10000)                    -> 00000000 00000000 00000000 00000000 -> 00000000
                0x80, 0x9F, 0x49,           // bpm  - varint 1200000 (120 * 10000)            -> 00000000 00010010 01001111 10000000 -> 10000000 10011111 01001001
                0x80, 0xB5, 0x18,           // time - varint 400000 (40 * 10000)              -> 00000000 00000110 00011010 10000000 -> 10000000 10110101 00011000
                0xC0, 0xB9, 0x55,           // bpm  - varint 1400000 (140 * 10000)            -> 00000000 00010101 01011100 11000000 -> 11000000 10111001 01010101
                0xC9, 0xAB, 0x2D,           // time - varint 742857 (74.(2857174) * 10000)    -> 00000000 00001011 01010101 11001001 -> 11001001 10101011 00101101
                0x80, 0x9F, 0x49            // bpm  - varint 1200000 (120 * 10000)            -> 00000000 00010010 01001111 10000000 -> 10000000 10011111 01001001
            ]);

            RagnaRuneString.Version1.RuneStringData expectedRuneStringData = new(
                [],
                [
                    new(120),
                    new(140, 40),
                    new(120, 74.28571428571429)
                ]
            );
            Assert.AreEqual(expectedRuneStringData, RuneStringSerializer.DeserializeV1(runeString));
        }

        [TestMethod]
        public void DeserializeMixed()
        {
            string runeString = Convert.ToBase64String([
                0x00, 0x01,                 // header
                0x02,                       // single rune count
                0xC4, 0x13,                 // varint 2500 (0.25 * 10000)  -> 00000000 00000000 00001001 11000100 -> 11000100 00010011
                0xCC, 0x3A,                 // varint 7500 (0.75 * 10000)  -> 00000000 00000000 00011101 01001100 -> 11001100 00111010
                0x20,                       // single rune column byte (00 10), with additional 4 bits of padding
                0x01,                       // double rune count
                0x88, 0x27,                 // varint 5000 (0.5 * 10000)   -> 00000000 00000000 00010011 10001000 -> 10001000 00100111
                0x60, 0x00, 0x00,           // double rune column combo bytes (011), with additional 21 bits of padding.
                0x01,                       // n-rune count
                0x90, 0x4E,                 // varint 10000 (1 * 10000)    -> 00000000 00000000 00100111 00010000 -> 10010000 01001110
                0x03,                       // n for n-rune
                0x00, 0x01, 0x02,           // column bytes (0, 1, 2)
                0x03,                       // BPM changes count
                0x00,                       // time - varint 0 (0 * 10000)                    -> 00000000 00000000 00000000 00000000 -> 00000000
                0x80, 0x9F, 0x49,           // bpm  - varint 1200000 (120 * 10000)            -> 00000000 00010010 01001111 10000000 -> 10000000 10011111 01001001
                0x80, 0xB5, 0x18,           // time - varint 400000 (40 * 10000)              -> 00000000 00000110 00011010 10000000 -> 10000000 10110101 00011000
                0xC0, 0xB9, 0x55,           // bpm  - varint 1400000 (140 * 10000)            -> 00000000 00010101 01011100 11000000 -> 11000000 10111001 01010101
                0xC9, 0xAB, 0x2D,           // time - varint 742857 (74.(2857174) * 10000)    -> 00000000 00001011 01010101 11001001 -> 11001001 10101011 00101101
                0x80, 0x9F, 0x49            // bpm  - varint 1200000 (120 * 10000)            -> 00000000 00010010 01001111 10000000 -> 10000000 10011111 01001001
            ]);

            RagnaRuneString.Version1.RuneStringData expectedRuneStringData = new(
                [
                    new(0.25, 0),
                    new(0.5, 1), new (0.5, 2),
                    new(0.75, 2),
                    new(1, 0), new(1, 1), new(1, 2)
                ],
                [
                    new(120),
                    new(140, 40),
                    new(120, 74.28571428571429)
                ]
            );
            Assert.AreEqual(expectedRuneStringData, RuneStringSerializer.DeserializeV1(runeString));
        }

        [TestMethod]
        public void DeserializeLongRuneString() => Assert.IsNotNull(RuneStringSerializer.DeserializeV1("AAG6CfeLBLufBP+yBMPGBIfaBMvtBI+BBdOUBZeoBdfgB5v0B9+HCKObCOauCKvCCO/VCLPpCPf8CLuQCf+jCcO3CYbLCcveCY7yCdKFCpaZCvbtDbqBDv6UDsKoDoa8DsrPDo7jDtL2DpaKD86bEZKvEdbCEd7pEeaQEu63Epb7E56iFOK1FKbJFK7wFLaXFfqqFb6+FcblFc6MFpKgFtazFt7aFuaBF6qVF+6oF/bPF7rjF/72F8KKGIeeGMqxGI7FGNLYGJbsGJ6TGeKmGaa6Ga7hGbaIGvqbGr6vGsbWGs79GpKRG9akG5q4G97LG6LfG+fyG6qGHO6ZHLKtHPbAHLrUHP7nHML7HIePHcqiHY62HdLJHZbdHZ6EHuKXHqarHq7SHrb5HvqMH76gH8bHH87uH5KCINaVIN68IObjIKr3IO6KIfaxIbrFIf7YIcLsIYeAIsqTIo6nItK6IpbOIp71IuKII6acI67DI7bqI/r9I76RJMa4JM7fJJLzJNaGJYbxJvf1Jo+YJ9OrJ5e/J5/mJ+P5J6eNKK+0KPPHKLfbKL+CKYOWKcepKc/QKZPkKdf3Kd+eKqOyKufFKu/sKrOAK/eTK/+6K8POK4fiK4+JLJewLJ/XLKf+LK+lLfO4LbfMLb/zLYOHLseaLs/BLpPVLtfoLt+PL6OjL+e2L+/dL7PxL/eEMP+rMMO/MIfTMI/6MNONMZehMZ/IMePbMafvMa+WMvOpMre9Mr/kMoP4MseLM8+yM5PGM9fZM9+ANKOUNOenNO/ONLPiNPf1NP+cNcOwNYfENY/rNZeSNp+5NqfgNq+HN/OaN7euN7/VN4PpN8f8N8+jOJO3ONfKON/xOKOFOeeYOe+/ObPTOffmOf+NOsOhOoe1Oo/cOtPvOpeDO5+qO+O9O6fRO6/4O/OLPLefPL/GPIPaPMftPM+UPde7Pd/iPaP2PeeJPu+wPrPEPvfXPv/+PsOSP4emP4/NP5f0P5+bQOOuQKfCQK/pQPP8QLeQQb+3QYPLQcfeQc+FQpOZQtesQt/TQqPnQuf6Qu+hQ7O1Q/fIQ//vQ8ODRIeXRI++RNPRRJflRJ+MReOfRaezRa/aRfPtRbeBRr+oRoO8RsfPRs/2RtedR9/ER6PYR+frR++SSLOmSPe5SP/gSMP0SIeISY+vSZfWSZ/9SeOQSqekSq/LSvPeSrfySr+ZS4OtS8fAS8/nS5P7S9eOTN+1TKPJTOfcTKvwTO+DTbOXTfeqTbu+Tf/RTcPlTYf5TcuMTo+gTtOzTpfHTq+8T8exUJe4U6+tVMeiVd+XVt/5X6ONYOegYO/HYPfuYP+VYYe9YY/kYafZYq+AY7enY7/OY8f1Y5+jZ6fKZ7eYaL+/aMfmaJ+UbKe7bK/ibLeJbb+wbcfXbZ+Fcaescbf6cb+hcsfIcv/ZdIeBdY+odaeddq/Edrfrdr+Sd8e5d5/neqeOe7fce7+DfMeqfJ/Yf6f/f6+mgAG3zYABv/SAAcebgQGfyYQBp/CEAbe+hQG/5YUBx4yGAf+diAGHxYgBl5OJAZ+6iQHjzYkBp+GJAa+IigG3r4oB+8KKAb/WigHH/YoBz6SLAZO4iwHXy4sB3/KLAeeZjAGrrYwB78CMAffnjAG7+4wB/46NAcOijQGHto0By8mNAY/djQHT8I0Bl4SOAZ+rjgHjvo4Bp9KOAa/5jgG3oI8B+7OPAb/HjwHH7o8Bz5WQAZOpkAHXvJABm9CQAd/jkAGj95AB54qRAauekQHvsZEBs8WRAffYkQG77JEB//+RAcOTkgGHp5IBy7qSAY/OkgHT4ZIBl/WSAZ+ckwHjr5MBp8OTAa/qkwG3kZQB+6SUAb+4lAHH35QBz4aVAZOalQHXrZUB39SVAef7lQGrj5YB76KWAffJlgG73ZYB//CWAcOElwGHmJcBy6uXAY+/lwHT0pcBl+aXAZ+NmAHjoJgBp7SYAa/bmAG3gpkB+5WZAb+pmQHH0JkBi+SZAc/3mQGTi5oB156aAYeJnAH4jZwBltecAZ7+nAHikZ0BpqWdAa7MnQHy350BtvOdAb6angGCrp4BxsGeAc7ongGS/J4B1o+fAd62nwGiyp8B5t2fAe6EoAGymKAB9qugAf7SoAHC5qABhvqgAY6hoQGWyKEBnu+hAaaWogGuvaIB8tCiAbbkogG+i6MBgp+jAcayowHO2aMBku2jAdaApAHep6QBorukAebOpAHu9aQBsomlAfacpQH+w6UBwtelAYbrpQGOkqYB0qWmAZa5pgGe4KYB4vOmAaaHpwGurqcB8sGnAbbVpwG+/KcBgpCoAcajqAHOyqgBkt6oAdbxqAHemKkBoqypAea/qQHu5qkBsvqpAfaNqgH+tKoBwsiqAYbcqgGOg6sBlqqrAZ7RqwGm+KsBrp+sAfKyrAG2xqwBvu2sAYKBrQHGlK0BzrutAZLPrQHW4q0B3omuAaKdrgHmsK4B7teuAbLrrgH2/q4B/qWvAcK5rwGGza8BjvSvAdKHsAGWm7ABnsKwAeLVsAGm6bABrpCxAfKjsQG2t7EBvt6xAYLysQHGhbIBzqyyAdbTsgHe+rIBoo6zAeahswHuyLMBstyzAfbvswH+lrQBwqq0AYa+tAGO5bQBloy1AZ6ztQHixrUBptq1Aa6BtgHylLYBtqi2Ab7PtgGC47YBxva2Ac6dtwGSsbcB1sS3Ad7rtwGi/7cB5pK4Ae65uAGyzbgB9uC4Af6HuQHCm7kBhq+5AY7WuQHS6bkBlv25AZ6kugHit7oBpsu6Aa7yugHyhbsBtpm7Ab7AuwGC1LsBxue7Ac6OvAHWtbwB3ty8AaLwvAHmg70B7qq9AbK+vQH20b0B/vi9AcKMvgGGoL4Bjse+AZbuvgGelb8B4qi/Aaa8vwGu478B8va/AbaKwAG+scABgsXAAcbYwAGK7MABzv/AAZKTwQHWpsEBlt/DAa7UxAHGycUBltDIAa7FyQHGusoB3q/LAd6R1QGipdUB5rjVAe7f1QH2htYB/q3WAYbV1gGO/NYBpvHXAa6Y2AG2v9gBvubYAcaN2QGeu9wBpuLcAbaw3QG+190Bxv7dAZ6s4QGm0+EBrvrhAbah4gG+yOIBxu/iAZ6d5gGmxOYBtpLnAb655wHG4OcBwoXqAYaZ6gHKrOoBjsDqAdLT6gGW5+oBno7rAbaD7AG+quwBxtHsAZ7/7wGmpvABtvTwAb6b8QHGwvEBnvD0AaaX9QGuvvUBtuX1Ab6M9gHGs/YBnuH5AaaI+gG21voBvv36Acak+wH+tf0Bht39AZ7S/gHi5f4Bpvn+Aa6g/wG2x/8B+tr/Ab7u/wHGlYACzryAApLQgALW44AC3oqBAuaxgQKqxYEC7tiBAvb/gQL+poICwrqCAobOggKO9YIClpyDAp7DgwLi1oMCpuqDAq6RhAK2uIQC+suEAr7fhALGhoUCzq2FApLBhQLW1IUC3vuFAuaihgKqtoYC7smGAvbwhgL+l4cCwquHAoa/hwKO5ocClo2IAp60iALix4gCptuIAq6CiQK2qYkC+ryJAr7QiQLG94kCzp6KApKyigLWxYoC3uyKAuaTiwKqp4sC7rqLAvbhiwL+iIwCwpyMAoawjAKO14wClv6MAp6ljQLiuI0CpsyNAq7zjQK2mo4C+q2OAr7BjgLG6I4Czo+PApKjjwLWto8C3t2PAuaEkAKqmJAC7quQAvbSkAL++ZACwo2RAoahkQKOyJEClu+RAp6WkgLiqZICpr2SAq7kkgK2i5MC+p6TAr6ykwLG2ZMCzoCUApKUlALWp5QC3s6UAub1lAKqiZUC7pyVAvbDlQL+6pUCwv6VAoaSlgKOuZYCluCWAp6HlwLimpcCpq6XAq7VlwK2/JcC+o+YAr6jmALGypgCzvGYApKFmQLWmJkC3r+ZAubmmQKq+pkC7o2aAva0mgL+25oCwu+aAoaDmwKOqpsCltGbAp74mwLii5wCpp+cAq7GnAK27ZwC+oCdAr6UnQLGu50CzuKdApL2nQLWiZ4C3rCeAubXngKq654C7v6eAvalnwL+zJ8CwuCfAob0nwKOm6AClsKgAtrVoAKe6aAC4vygAqaQoQLqo6ECrrehAvLKoQK23qEC+vGhAr6FogKCmaICxqyiAorAogLO06ICkueiAtb6ogKajqMC3qGjAqK1owLmyKMCqtyjAu7vowKyg6QC9pakArqqpAL+vaQCwtGkAoblpALK+KQCjoylAtKfpQKWs6UCvvamAsadpwLOxKcC1uunAt6SqAKipqgC5rmoAu7gqAL2h6kC/q6pAobWqQKO/akCpvKqAuqFqwKumasC8qyrArbAqwL606sCvuerAoL7qwLGjqwCiqKsAs61rAKSyawC1tysAvb4rQLn/a0ClpWvAr7YsALG/7ACzqaxAtbNsQLuwrIC/pCzAsKkswKGuLMCysuzAo7fswLS8rMCloa0AraitQL6tbUCvsm1AoLdtQLG8LUCioS2As6XtgKSq7YC1r62AprStgLe5bYCovm2AuaMtwKqoLcC7rO3ArLHtwL22rcCuu63Av6BuALClbgChqm4Asq8uAKO0LgC0uO4Apb3uAK2k7oCwce6Asv7ugLVr7sC9su8Ao7BvQKW6L0CtoS/AvqXvwK+q78Cgr+/AsbSvwKK5r8Czvm/ApKNwALWoMACmrTAAt7HwAKi28AC5u7AAqqCwQLulcECsqnBAva8wQK60MEC/uPBAsL3wQKGi8ICyp7CAo6ywgLSxcICltnCArb1wwLBqcQCy93EAtWRxQLXkcUClsrHAtrdxwKe8ccC4oTIAqaYyALqq8gCrr/IAvLSyAK25sgC+vnIAr6NyQKCockCxrTJAorIyQLO28kCku/JAtaCygKalsoC3qnKAqK9ygLm0MoCquTKAu73ygKyi8sC9p7LArqyywL+xcsCwtnLAobtywLKgMwCjpTMAtKnzAL2j9ACuqPQAv620ALCytACh97QAsrx0AKOhdEC0pjRApas0QLW5NMCmvjTAt6L1AKin9QC57LUAqrG1ALu2dQCsu3UAvaA1QK6lNUC/qfVAsK71QKHz9UCyuLVAo721QLSidYClp3WAvbx2QK6hdoC/pjaAsKs2gKHwNoCytPaAo7n2gLS+toClo7bAsb43AKKjN0Czp/dApKz3QLWxt0C3u3dAuaU3gLuu94C9uLeAv6J3wKGsd8CjtjfAuaF4wLurOMC9tPjAv764wKGouQC3s/nAub25wL2xOgC/uvoAoaT6QLewOwC5ufsAu6O7QL2te0C/tztAoaE7gLesfEC5tjxAvam8gL+zfIChvXyAr6G9QLGrfUCztT1AubJ9gLu8PYC9pf3Av6+9wKG5vcC3pP7Aua6+wL2iPwC/q/8AobX/ALehIAD5quAA+7SgAP2+YAD/qCBA4bIgQPe9YQD5pyFA/bqhQP+kYYDhrmGA77KiAPG8YgD1r+JA97miQOi+okD5o2KA+60igP224oDuu+KA/6CiwOGqosDjtGLA9LkiwOW+IsDnp+MA6bGjAPq2YwDru2MA7aUjQP6p40DvruNA4LPjQPG4o0DivaNA86JjgOSnY4D1rCOA97XjgOi644D5v6OA+6ljwP2zI8DuuCPA/7zjwOGm5ADjsKQA9LVkAOW6ZAD2vyQA56QkQPio5EDpreRA+rKkQOu3pED8vGRA7aFkgP6mJIDvqySA4LAkgPG05IDiueSA876kgOSjpMD1qGTA97IkwOi3JMD5u+TA+6WlAP2vZQDutGUA/7klAOGjJUDjrOVA9LGlQOW2pUDnoGWA6aolgPqu5YDrs+WA7b2lgP6iZcDvp2XA4KxlwPGxJcDitiXA87rlwOS/5cD1pKYA965mAOizZgD5uCYA+6HmQP2rpkDusKZA/7VmQOG/ZkDypCaA46kmgPSt5oDlsuaA9aDnQOal50D3qqdA+74nQOyjJ4D9p+eAxu5Bu5G7kG7kEAxMSERBu5Ozt7kRuRGxMSERBu5Ozt7RPtFJFFFOZRRT75RT7RSRRRTmUUU++UUmZmdmZmJmZme7hEZmZnZmZiZmZnthNjYtu7kmZjZREFkaZjZREFkcTEhEQbuTs7e5EbkRsTEhEQbuTs7SdPtFJFFFOZRRT75RT7RSRRRTmUUU++UUmZmdmZmJmZme7hEZmZnZmZiZmZmS27uSZmNlEQWbYmY2URBZFnmGZkSJnmGZkSJnmGZkSJnmGZkSJnmGZkSJnmGZkSJnmGZkSESJne2ISY2NobmLBJnG6wbknvu5JycnuRG47t3ZiIRJwbZGZGZHm5ubmG7kG7kbuQbuTkqqqZjZREFkaZjZREFkcTEhEQbuTs7e5EbkRsTEhEQbuTs7SdmYKoBljfG9BD23hL+hROGrROO1BPnzVHv9FH3m1L/wlKH6lKH21ePgliXqVi/7FnHk1rPulqHzFyP81yXml23tl6/3V7HhF/Pq1+Xi2LPnGTf6mTvuGX/hmaP1WbPjWnf22nvqWr/92qPxmvP/m3fzG7vmm//6G+Pt3DP73LfvXPvi3SXz3XP4Hffrnjv/Hj/ynmPmXrP0Xzfn33v7X3/u36Pin/PwoEB35CCAe/eggH/rIMBj/uDAc+zhgHfgYcB78+HAeblxgHujMcB9rPHAf7axwGGgsgBhvPMAY6azQGWwc0BvoTPAcarzwHO0s8BhuTRAY6L0gGWstIBts7TAb710wHGnNQBzsPUAZaj1wHOtNkB3oLaAe7Q2gH+ntsBju3bAc6l3gHe894B7sHfAf6P4AGO3uABzpbjAd7k4wHusuQB/oDlAY7P5QHOh+gB3tXoAe6j6QGmtesBzvjsAd7G7QHulO4B/uLuAY6x7wHO6fEB3rfyAe6F8wH+0/MBjqL0Ac7a9gHeqPcB7vb3Af7E+AGOk/kBzsv7Ad6Z/AHu5/wBlqv+AfatxgKWu8wClv/fAp6m4AKmzeACrvTgArab4QK+wuECxunhAs6Q4gLWt+ICjsnkAp6X5QKu5eUCvrPmAs6B5wKOuukCnojqAq7W6gK+pOsCzvLrAo6r7gKe+e4CrsfvAr6V8ALO4/ACjpzzAp7q8wKuuPQC1vv1Ao6N+AKe2/gCrqn5Ar73+QLOxfoCjv78Ap7M/QKumv4Cvuj+As62/wKO74EDnr2CA66LgwO+2YMDzqeEA47ghgOerocDrvyHA6aZmwO255sDxrWcA1QAA66Cy45bbUKY7GRDRawhTHYyIaLRQ66Cy45bbUKY7GRDRawhTHYyIaLRUU222mhTHYyIaLWEKY7GRDRaLGQAAAACALDNXpY3sM1e"));

        [TestMethod]
        public void DeserializeInvalidRuneString() => Assert.ThrowsException<ArgumentException>(() => RuneStringSerializer.DeserializeV1("test"));

        [TestMethod]
        public void DeserializeWrongVersion() => Assert.ThrowsException<ArgumentException>(() =>
            RuneStringSerializer.DeserializeV1(RuneStringSerializer.Serialize(new RagnaRuneString.Version1.RuneStringData([], []), RagnaRuneString.Version.VERSION_0)));
    }
}