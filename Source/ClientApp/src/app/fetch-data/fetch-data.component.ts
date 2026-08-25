import { Component } from '@angular/core';
import { NgFor, NgIf } from '@angular/common';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-fetch-data',
  standalone: true,
  imports: [NgFor, NgIf],
  templateUrl: './fetch-data.component.html',
  styleUrls: ['./fetch-data.component.css']
})
export class FetchDataComponent {
  public review?: CustomerReview;
  public isLoading = false;
  public hasError = false;

  constructor(private http: HttpClient) {
    this.generate();
  }

  /**
   * Call the generate endpoint to get a newly constructed review.
   * @returns new CustomerReview object
   */
  public generate() {
    this.review = undefined;
    this.isLoading = true;
    this.hasError = false;
    return this.http.get<CustomerReview>('api/generate')
      .subscribe({
        next: (result) => {
          this.review = result;
          this.isLoading = false;
        },
        error: (error: unknown) => {
          this.review = undefined;
          this.isLoading = false;
          this.hasError = true;
          console.error(error);
        }
      });
  }
}

interface CustomerReview {
  summary: string;
  rating: number;
}
