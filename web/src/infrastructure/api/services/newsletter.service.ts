export interface NewsletterSubscribeRequest {
  email: string;
  metroAreaIds?: string[];
  receiveAllLocations: boolean;
}

export interface NewsletterSubscribeResponse {
  success: boolean;
  message?: string;
  subscriberId?: string;
  error?: string;
  code?: string;
}

export const newsletterService = {
  async subscribe(data: NewsletterSubscribeRequest): Promise<NewsletterSubscribeResponse> {
    const response = await fetch('/api/newsletter/subscribe', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        ...data,
        timestamp: new Date().toISOString(),
      }),
    });

    return response.json();
  },
};
