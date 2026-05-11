export interface Customer {
  id: string;
  code: string;
  name: string;
  contactPerson?: string;
  contactEmail?: string;
  contactPhone?: string;
  note?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface CreateCustomerRequest {
  code: string;
  name: string;
  contactPerson?: string;
  contactEmail?: string;
  contactPhone?: string;
  note?: string;
}

export interface UpdateCustomerRequest {
  name: string;
  contactPerson?: string;
  contactEmail?: string;
  contactPhone?: string;
  note?: string;
}
