# ⛏️ Mining Order Management System (MOMS)

The **Mining Order Management System (MOMS)** is a cloud-based web application developed to automate and streamline **material ordering, inventory management, and supplier coordination** within mining operations.

The system was developed for **Khula Mining Corporation**, operating in remote mining locations in Limpopo, South Africa, where manual order processing caused delays, data loss, and inefficiencies.

---

## 📌 Project Details

- **Module:** XADAD7112  
- **Project Type:** Team Project  
- **Industry:** Mining (Surface & Underground)  
- **Location:** Limpopo, South Africa  

### 👥 Team Members

| Name | Student Number |
|----|----|
| Phaka Phuti Thabiso | ST10219717 |
| Sykvester Netswinganani | ST10193141 |

---

## 🎯 Purpose of the System

MOMS was created to:
- Replace paper-based and phone-based order handling
- Improve coordination between Operations, Procurement, and Warehouse departments
- Enable real-time inventory tracking
- Reduce delays, duplicate orders, and downtime
- Provide secure access through role-based dashboards

---

## 🏭 Problem Statement

Manual processes such as paperwork and verbal requests resulted in:
- Lost or duplicated orders
- Inaccurate inventory records
- Delayed supplier deliveries
- No real-time reporting
- Increased operational costs

---

## 💡 Solution Overview

MOMS provides a **centralized digital platform** that allows:
- Supervisors to request materials and equipment
- Admin users to approve orders and manage inventory
- Suppliers to update delivery status and upload invoices
- Warehouse staff to track stock movement in real time

The system supports **desktop and mobile access**, making it suitable for remote mining environments.

---

## 🧩 Key Features

### 👤 User & Role Management
- Secure login and authentication
- Role-based access control:
  - Admin
  - Supervisor
  - Supplier

### 📦 Material & Order Management
- Create and track material/equipment requests
- Approval workflow:
  `Pending → Approved → Declined → Dispatched → Delivered`
- Multi-level approval for high-cost items

### 🏬 Inventory Management
- Real-time stock tracking
- Low-stock alerts
- Warehouse inflow and outflow tracking

### 🚚 Supplier Management
- Supplier profile management
- Purchase order assignment
- Delivery status updates
- Invoice uploads

### 📊 Reporting & Analytics
- Inventory and order reports
- Supplier performance analysis
- Export reports to PDF and Excel

### 🔔 Notifications
- Email/SMS alerts for:
  - New requests
  - Approval decisions
  - Low stock warnings
  - Delivery updates

### 📱 Mobile-Friendly Interface
- Responsive UI using Bootstrap
- Optimized for tablets and mobile browsers

---

## 🏛️ System Architecture

MOMS follows a **Layered Architecture** using MVC principles:

1. Client Layer (Web & Mobile UI)  
2. Presentation Layer (Controllers & ViewModels)  
3. Business Services Layer  
4. Data Access Layer (Entity Framework Core)  
5. External Services Layer (Notifications & Storage)

---

## 🛠️ Technology Stack

| Layer | Technology |
|----|----|
| Backend | ASP.NET Core MVC |
| Language | C# |
| ORM | Entity Framework Core |
| Frontend | Razor Views, Bootstrap 5 |
| Database | Azure SQL Database |
| Authentication | Cookie-based Authentication |
| Cloud Platform | Microsoft Azure |
| Version Control | GitHub |
| Project Management | Azure DevOps |

---

## 🔐 Security & Data Integrity

- Cookie-based authentication with HTTP-only secure cookies
- Role-based authorization
- Password hashing
- HTTPS enforcement
- SQL Injection prevention using EF Core
- Server-side and client-side validation
- Centralized error handling and logging

---


---

## ☁️ Cloud & Database

- **Database:** Microsoft Azure SQL Database  
- **Benefits:**
  - Automatic backups
  - High availability
  - Secure cloud storage
  - Real-time multi-site access

---

## 🚧 Out of Scope

- Payroll and accounting systems
- HR management
- GPS vehicle tracking
- Supplier price negotiation tools

---

## 📈 Future Enhancements

- Native mobile application
- Predictive analytics
- ERP integration
- Advanced offline support
- AI-driven reporting

---

## 📚 Academic Value

This project demonstrates:
- Enterprise application development
- Secure authentication and authorization
- Cloud-based database integration
- Agile team collaboration
- Real-world mining operations problem solving

---

## 🔗 Project Links

- **GitHub Repository:**
- https://github.com/phutithabiso/MiningOps.git
- https://github.com/Netswinganani/MiningOps.git  

- **Azure DevOps Dashboard:**  
  https://dev.azure.com/ST10219717/Mining%20Order%20Operation%20Management

---
