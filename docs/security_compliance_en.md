## 1. ISO/IEC 27001:2022 Official Certification

According to the official international compliance certificate issued by the independent auditor Schellman Compliance, LLC on December 12, 2025, Microsoft Azure's Information Security Management System (ISMS) fully complies with the ISO/IEC 27001:2022 standard.

> **Certificate Details:**
>
> * **Certificate Number**: 1729711-17
> * **Scope**: Development, operation, and infrastructure of Azure Public Cloud

The services selected by our team for this project are officially included in the certified scope (In-Scope Services):

* **Microsoft Foundry: Azure Language** (Responsible for extracting Personal Identifiable Information - PII attributes)
* **Microsoft Foundry: Azure Speech** (Used for audio transcription, if applicable)

## 2. Data Residency

To comply with the company's privacy requirements and ISO 27001 principles, all Azure AI resources are deployed exclusively in the West Europe region.

According to the appendix of the certificate (pages 18–21), Microsoft's European data centers have passed a full security audit:

* **Primary Processing Hub**: Amsterdam, Netherlands (Amsterdam, Netherlands — In-Scope Datacenter)
* Data processed during execution does not leave the boundaries of the European Union (EU).

## 3. Data Handling & Security Rules

Throughout the data processing lifecycle, a high level of confidentiality and protection is maintained using the following methods:

* **Data In Transit Security** — All communication between the user and the Azure AI service is transmitted exclusively using encrypted TLS 1.2 or TLS 1.3 protocols. This prevents any traffic eavesdropping by third parties.
* **Data At Rest Security** — If any temporary cached data occurs within the system, it is automatically encrypted at the server level using the advanced AES-256 standard.
* **Stateless PII Processing** — Our selected Azure AI Language API operates in Stateless mode (without retaining state). After extracting names, addresses, and other Personally Identifiable Information (PII) from the text and returning the JSON response, all logs and data are instantly wiped from the operational memory. The data is not stored on disk and is not used by Microsoft to train artificial intelligence models.

## 4. Business Requirements Alignment

The implemented system fully satisfies the acceptance criteria described in the project's technical specifications:
1. **Customer Privacy**: Names, addresses, and Social Security Numbers (SSN) are processed in an isolated environment, eliminating data leakage risks.
2. **No Third-Party Data Retention**: Since Microsoft does not store stateless requests, the company's corporate information remains strictly within our application lifecycle.
