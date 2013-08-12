/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit developer.mindtouch.com;
 * please review the licensing section.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License along
 * with this program; if not, write to the Free Software Foundation, Inc.,
 * 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 * http://www.gnu.org/copyleft/gpl.html
 */

using System;
using System.Collections.Generic;
using System.Text;

using NUnit.Framework;
using MindTouch.Dream;
using MindTouch.Xml;

namespace MindTouch.Deki.Tests.RatingTests {

    [TestFixture]
    public class RatingTests {

        /// <summary>
        ///     Retrieve page rating of an unrated page
        /// </summary>        
        /// <feature>
        /// <name>GET:pages/{pageid}/ratings</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/GET%3apages%2f%2f%7Bpageid%7D%2f%2fratings</uri>
        /// </feature>
        /// <expected>Return document contains correct rating information</expected>

        [Test]
        public void Ratings_GetRatingInfoForUnratedPage_NoData()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();

            // Create random page
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            // Retrieve rating info for unrated page and assert correct data is returned
            msg = p.At("pages", id, "ratings").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page rating retrieval failed!");
            Assert.AreEqual(String.Empty, msg.ToDocument()["@score"].AsText, "Score attribute contains a value!");
            Assert.AreEqual(0, msg.ToDocument()["@count"].AsInt, "Unrated page count does not equal 0!");
        }

        /// <summary>
        ///     Rate a page with score '1', then score '0', then unrate page
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/ratings</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3apages%2f%2f%7Bpageid%7D%2f%2fratings</uri>
        /// <parameter>score</parameter>
        /// </feature>
        /// <expected>Return document contains correct rating information</expected>

        [Test]
        public void RatePage() {

            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();
            
            // Create random page
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);
            
            // Rate page a '1'
            msg = p.At("pages", id, "ratings").With("score", 1).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page rating did not return OK: " + msg.ToString());

            // Check to see if returned document elements/attributes are correct
            Assert.AreEqual(1, msg.AsDocument()["/rating/@score"].AsInt, "Unexpected computed rating");
            Assert.AreEqual(1, msg.AsDocument()["/rating/@count"].AsInt, "Unexpected rating count");
            Assert.AreEqual(1, msg.AsDocument()["/rating/user.ratedby/@score"].AsInt, "Unexpected user rating");

            // Rate page a '0'
            msg = p.At("pages", id, "ratings").With("score", 0).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page rating did not return OK: " + msg.ToString());

            // Check to see if returned document elements/attributes are correct
            Assert.AreEqual(0, msg.AsDocument()["/rating/@score"].AsInt, "Unexpected computed rating");
            Assert.AreEqual(1, msg.AsDocument()["/rating/@count"].AsInt, "Unexpected rating count");
            Assert.AreEqual(0, msg.AsDocument()["/rating/user.ratedby/@score"].AsInt, "Unexpected user rating");

            // Clear page rating
            msg = p.At("pages", id, "ratings").With("score", String.Empty).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page rating did not return OK: " + msg.ToString());

            // Check to see if returned document elements/attributes are correct
            Assert.AreEqual(String.Empty, msg.ToDocument()["@score"].AsText, "Score attribute contains a value!");
            Assert.AreEqual(0, msg.ToDocument()["@count"].AsInt, "Unrated page count does not equal 0!");

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Rate a page with an invalid score
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/ratings</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3apages%2f%2f%7Bpageid%7D%2f%2fratings</uri>
        /// <parameter>score</parameter>
        /// </feature>
        /// <expected>400 Bad Request HTTP response</expected>

        [Test]
        public void RatePageInvalidScore() {
            
            // Build ADMIN plug 
            Plug p = Utils.BuildPlugForAdmin();

            // Create a random page
            string id = null;
            string path = null;
            DreamMessage msg = PageUtils.CreateRandomPage(p, out id, out path);

            // Attempt to rate a page without the query parameter 'score'
            msg = p.At("pages", id, "ratings").PostAsync().Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "Page rating did not return 400: " + msg.ToString());

            // Attempt to rate page a '2' and assert a 'Bad Request' HTTP response is returned
            msg = p.At("pages", id, "ratings").With("score", 2).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "Page rating did not return 400: " + msg.ToString());

            // Attempt to rate page a '0.5' and assert a 'Bad Request' HTTP response is returned
            msg = p.At("pages", id, "ratings").With("score", 0.5).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.BadRequest, msg.Status, "Page rating did not return 400: " + msg.ToString());

            // Delete the page
            PageUtils.DeletePageByID(p, id, true);
        }

        /// <summary>
        ///     Have USER_COUNT number of users rate a specific page with a randomly chosen '0' or '1' score. User then has a 1:PROB chance to unrate page immediately after making initial rating.
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/ratings</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3apages%2f%2f%7Bpageid%7D%2f%2fratings</uri>
        /// <parameter>score</parameter>
        /// </feature>
        /// <expected>Final retrieved ratings score matches the calculated average</expected>

        [Test]
        public void Ratings_ManyUsersRandomlyRatePage_CorrectRating()
        {
            // Build ADMIN plug
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;

            // Create a page
            string pageid = null;
            PageUtils.CreateRandomPage(p, out pageid);

            // Number of users to create, each rating the page
            int USER_COUNT = 5;
            string userid = null;
            string username = null;

            // Variables to calculate average 
            int score = 0;
            int sum = 0;
            int count = 0;
            float average = 0.0f;

            // User has 1 in PROB chance to immediately unrate previous rating
            int PROB = 4;
            Random random = new Random();

            for (int i = 0; i < USER_COUNT; i++)
            {
                // Log in as ADMIN
                p = Utils.BuildPlugForAdmin();

                // Create a random Contributor
                UserUtils.CreateRandomContributor(p, out userid, out username);

                // Log in as Contributor
                p = Utils.BuildPlugForUser(username, "password");

                // Randomly generate a value of '0' or '1'
                score = random.Next(0, 2);
                sum += score;
                count++;

                // Submit generated value as score
                msg = p.At("pages", pageid, "ratings").With("score", score).PostAsync().Wait();
                Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page rating failed!");

                // Randomly unrate page (1/PROB chance)
                if (random.Next(0, PROB) == 0)
                {
                    msg = p.At("pages", pageid, "ratings").With("score", String.Empty).PostAsync().Wait();
                    Assert.AreEqual(DreamStatus.Ok, msg.Status, "Unrating the page failed!");
                    sum -= score;
                    count--;
                }
            }

            // Log in as ADMIN
            p = Utils.BuildPlugForAdmin();

            // Retrieve page rating
            msg = p.At("pages", pageid, "ratings").GetAsync().Wait();
            Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page rating retrieval failed!");

            // Assert the average score retrieived matches the calculated average below
            if (count > 0)
            {
                average = (float)sum / (float)count;
                Assert.AreEqual(Math.Round(average, 3), Math.Round(msg.ToDocument()["@score"].AsFloat ?? 0, 3), "Retrieved rating does not the calculated expected rating!");
                Assert.AreEqual(count, msg.ToDocument()["@count"].AsInt, "Unexpected count!");
            }
            else
            {
                Assert.AreEqual(String.Empty, msg.ToDocument()["@score"].AsText, "Retrieved rating does not the calculated expected rating!");
                Assert.AreEqual(count, msg.ToDocument()["@count"].AsInt, "Unexpected count!");
            }

            // Delete the page
            PageUtils.DeletePageByID(p, pageid, true);
        }

        /// <summary>
        ///     Rate a page many times with random ratings ('0', '1', String.Empty) through a single user
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/ratings</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3apages%2f%2f%7Bpageid%7D%2f%2fratings</uri>
        /// <parameter>score</parameter>
        /// </feature>
        /// <expected>Page is rated correctly</expected>

        [Test]
        public void Ratings_UserRandomlyRatesPageManyTimes_CorrectRating()
        {
            // Log in as ADMIN
            Plug p = Utils.BuildPlugForAdmin();
            DreamMessage msg;

            // Create a page
            string pageid = null;
            PageUtils.CreateRandomPage(p, out pageid);

            // Number of times to randomly rate page
            int RATE_COUNT = 25;
            Random random = new Random();
            int score = 0;

            for (int i = 0; i < RATE_COUNT; i++)
            {
                // Values 0, 1, 2 possible. 2 is empty string.
                score = random.Next(0, 3);
                switch (score)
                {
                    case 0:
                        msg = p.At("pages", pageid, "ratings").With("score", score).PostAsync().Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page rating failed!");
                        Assert.AreEqual(0, msg.AsDocument()["/rating/@score"].AsInt, "Unexpected computed rating");
                        Assert.AreEqual(1, msg.AsDocument()["/rating/@count"].AsInt, "Unexpected rating count");
                        Assert.AreEqual(0, msg.AsDocument()["/rating/user.ratedby/@score"].AsInt, "Unexpected user rating");
                        break;
                    case 1:
                        msg = p.At("pages", pageid, "ratings").With("score", score).PostAsync().Wait();
                        Assert.AreEqual(DreamStatus.Ok, msg.Status, "Page rating failed!");
                        Assert.AreEqual(1, msg.AsDocument()["/rating/@score"].AsInt, "Unexpected computed rating");
                        Assert.AreEqual(1, msg.AsDocument()["/rating/@count"].AsInt, "Unexpected rating count");
                        Assert.AreEqual(1, msg.AsDocument()["/rating/user.ratedby/@score"].AsInt, "Unexpected user rating");
                        break;
                    case 2:
                        msg = p.At("pages", pageid, "ratings").With("score", String.Empty).PostAsync().Wait();
                        Assert.AreEqual(String.Empty, msg.ToDocument()["@score"].AsText, "Score attribute contains a value!");
                        Assert.AreEqual(0, msg.ToDocument()["@count"].AsInt, "Unrated page count does not equal 0!");
                        break;
                    default:
                        break;
                }
            }

            // Delete the page
            PageUtils.DeletePageByID(p, pageid, true);
        }

        /// <summary>
        ///     Rate a page through anonymous
        /// </summary>        
        /// <feature>
        /// <name>POST:pages/{pageid}/ratings</name>
        /// <uri>http://developer.mindtouch.com/en/ref/MindTouch_API/POST%3apages%2f%2f%7Bpageid%7D%2f%2fratings</uri>
        /// <parameter>score</parameter>
        /// </feature>
        /// <expected>401 Unauthorized HTTP response</expected>

        [Test]
        public void Ratings_VoteThroughAnonymous_Unauthorized()
        {
            // Login as ADMIN and create a page
            Plug p = Utils.BuildPlugForAdmin();
            string pageid = null;
            PageUtils.CreateRandomPage(p, out pageid);

            // Login as anonymous
            p = Utils.BuildPlugForAnonymous();

            // Rate page through anonymous
            DreamMessage msg = p.At("pages", pageid, "ratings").With("score", 1).PostAsync().Wait();
            Assert.AreEqual(DreamStatus.Unauthorized, msg.Status, "Anonymous succeeded in rating page!");

            // Login as ADMIN and delete page
            p = Utils.BuildPlugForAdmin();
            PageUtils.DeletePageByID(p, pageid, true);
        }
    }
}
