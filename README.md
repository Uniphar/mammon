# Mammon

## Table of Contents

- [Mammon](#mammon)
  - [Table of Contents](#table-of-contents)
  - [Product Vision](#product-vision)
  - [Mission Statement](#mission-statement)
  - [Value Proposition](#value-proposition)
  - [Roadmap](#roadmap)
  - [Utility Projects](#utility-projects)
  - [Releases](#releases)
  - [Backlog](#backlog)

Mammon is a DevOps solution developed by the Uniphar DevOps team.  
It is designed to generate detailed cost reports from Azure, applying custom rules to allocate costs to the appropriate cost centers.  
This solution addresses the challenge of allocating costs for resources that are shared among multiple projects, such as AKS, SQL Server, VDIs, and DevBoxes

## Product Vision

To deliver a cost tracking solution for cloud services that embodies Uniphar’s dedication to innovation, efficiency, and value creation.

This vision emphasizes providing a comprehensive, automated, and user-friendly tool while focusing on accountability and cost savings, which are crucial aspects of the product’s value proposition

## Mission Statement

The mission of Mammon is to equip stakeholders with transparent, real-time insights into Azure resource utilization.  
This fosters informed decision-making, resource optimization, and cost-effective management, aligning with Uniphar’s ethos of innovation, efficiency, and value creation

## Value Proposition

Mammon retrieves the costs of every resource in Azure and applies custom rules to allocate their respective costs to the proper cost center.  
This is particularly important because, in our context, Azure does not provide a way to identify clearly which resources belong to whom or in which scope they were created.  
Mammon uses knowledge of how resources are created, specifically naming conventions, and applies custom-defined rules to identify, group, and calculate costs.  
This includes calculating costs for resources uniquely used by a particular division or project and pro-rata costs for shared resources.

## Roadmap

| Now                                                                                | Next                         | Future                                                                                                                                         |
| ---------------------------------------------------------------------------------- | ---------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| - Azure Subscriptions Cost Allocation </br> &nbsp;&nbsp;- Reporting </br> &nbsp;&nbsp;- Resource cost allocation </br></br></br>| - User Access </br> - Dashboards </br></br></br></br>| - Enhanced data visualization. </br> - Automated report generation. </br> - Additional integrations </br> - Advanced analytics </br> - Make it public \* |

\* Either make it public and open source, or allow other business units/partners to use it too.

## Utility Projects

[Mammon Cost Centre Mapping](https://github.com/Uniphar/MammonCostCentreMapping) - This project is a utility project that allows you to map cost centers to Azure resources and shared resources.

## Releases

| release | date | description |
| ------- | ---- | ----------- |
| v 0.0.1 - prototype | 2024-05-11 | Initial prototype |
| v 0.0.2 - prototype | 2024-05-15 | added regex support and report improvements |
| v 0.0.3 - prototype | 2024-05-22 | send email with cost report |

## Backlog
[Mammon Azure Devops Backlog](https://dev.azure.com/UnipharGroup/1/_backlogs/backlog/DevOps/Epics?showParents=false&System.AreaPath=Mammon)
