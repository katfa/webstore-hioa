﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Data;
using Webstore.Models;
using System.Data.Linq;

namespace Webstore
{
    public class DB
    {
        private DataClassesDataContext db = new DataClassesDataContext();

        public byte[] generateHash(string password)
        {
            if (password != null)
            {
                var algorithm = SHA256.Create();
                return algorithm.ComputeHash(Encoding.ASCII.GetBytes(password));
            }
            else
                return null;
        }

        public string insertCustomer(Webstore.Models.customer customer)
        {
            try
            {
                var zip = from z in db.cities
                            where z.zipcode == customer.zipcode
                            select z;

                if (zip.Count() == 0)
                {
                    return "This is not a norwegian zipcode";
                }
                db.customers.InsertOnSubmit(customer);
                db.SubmitChanges();
                return "You've been registered! You will now be redirected back to the store";
            }
            catch
            {
                return "Something went wrong. Try again later.";
            }
        }

        public Dictionary<int, List<string>> LogIn(string email, string password)
        {
            byte[] hashedPassword = generateHash(password);

            var user = from c in db.customers
                           where email == c.email && hashedPassword == c.password
                           select c;


            if (user.Count() > 0)
            {
                Dictionary<int, List<string>> customer = new Dictionary<int, List<string>>();
                List<string> properties = new List<string>();

                properties.Add(user.First().firstname);
                properties.Add(user.First().lastname);
                properties.Add(user.First().email);
                customer.Add(user.First().Id, properties);

                return customer;
            }
            else
            {
                return null;
            }
        
        }

        private List<EntitySet<orderdetail>> getCustomerOrders(int customerId)
        {
            List<EntitySet<orderdetail>> orders = new List<EntitySet<orderdetail>>();

            var orderList = from o in db.orders
                                where customerId == o.customerID
                                select o.orderdetails;

            foreach (var item in orderList)
            {
                orders.Add(item);
            }

            return orders;
        }

        public Dictionary<int, Dictionary<string, string>> getCustomerOrderDetails(int customerId)
        {
           
            Dictionary<int, Dictionary<string, string>> orderDetailList = new Dictionary<int, Dictionary<string, string>>();

            decimal priceTotal;
            foreach (var item in getCustomerOrders(customerId))
            {
                    

                foreach (var property in item)
                {
                    Dictionary<string, string> details = new Dictionary<string, string>();
                    var product = from p in db.products
                                        where property.productID == p.Id
                                        select new {p.name, p.price};

                    var date = from o in db.orders
                                where o.Id == property.orderID
                                select new { o.date };

                    priceTotal = product.First().price * property.quantity;
                    details.Add("productName", product.First().name);
                    details.Add("priceTotal", Convert.ToString(priceTotal));
                    details.Add("date", Convert.ToString(date.First()));

                    orderDetailList.Add(property.Id, details);

                }
               
            }
            return orderDetailList;
        }

        public order insertOrder(int customerId)
        {
            order o = new order
            {
                date = DateTime.Now,
                customerID = customerId
            };
            db.orders.InsertOnSubmit(o);
            try
            {
                db.SubmitChanges();
                return o;
            }
            catch
            {
                return null;
            }

        }

        public bool insertOrderDetails(int orderId, int pId, int amount)
        {
            orderdetail od = new orderdetail { 
                orderID = orderId,
                productID = pId,
                quantity = amount
            };

            db.orderdetails.InsertOnSubmit(od);
            try
            {
                db.SubmitChanges();
                return true;
            }
            catch
            {
                return false;
            }
           
        }
        public List<Models.product> getAllProducts()
        {
            return db.products.ToList();
        }

        public Dictionary<int, string> getAllCategories()
        {
            Dictionary<int, string> categories = new Dictionary<int, string>();

            var cat = from c in db.categories
                      select c;

            foreach (var c in cat)
            {
                categories.Add(c.Id, c.name);
            }
            return categories;
        }

        public product getProduct(int id)
        {
           return db.products.Where(p => id == p.Id).Single();
        }
    }
}