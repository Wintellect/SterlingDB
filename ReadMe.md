# Sterling NoSQL OODB #

Sterling is a lightweight NoSQL object-oriented database for .Net 4.5, Windows Store, and Windows Phone 8 that works with your existing class structures. Sterling supports full LINQ to Object queries over keys and indexes for fast retrieval of information from large data sets.

The goal behind Sterling is to keep it:
  - Non-intrusive. You shouldn't have to change your classes just to persist them. (No awkward mapping from a class model to a relational database model).
  - Lightweight. As of this writing, the total memory footprint of Sterling is under 100 Kb. No one needs to bloat their project for something as simple as persisting data.
  - Flexible. While the core is light, Sterling is designed to handle any serialization task and make it ultra-easy to query databases using LINQ-to-Objects.
  - Portable. Sterling runs equally well on the desktop/server .NET 4.5 Framework, Windows Store, and Windows Phone 8.

Important Note from Jeremy Likness 

Using Sterling comes with a trade-off. This product is maintained by a very small team - mostly Jeremy Likness with some assistance - so there is no guaranteed turnaround time for bug-fixes and there is no support hotline. That is the risk: defects may exist, and there is no guaranteed resolution. The reward is an out-of-the-box tool that makes saving and serializing objects very fast, easy, and straightforward. To further remove any barriers to use, I purposefully make two decisions with this project: first, you have 100% uncompromised access to the source at no cost, and second, you have full rights to take the source, modify it, fix it, or use it however you see fit within your own projects, at your own risk, against with no charge. There is no commercial fee for this project. I hope that this risk/reward model works for you and I also look forward to anyone open to joining our team to help support this product through future releases.
