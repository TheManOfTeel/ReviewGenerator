import { Component } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-fetch-data',
  standalone: false,
  templateUrl: './fetch-data.component.html',
  styleUrls: ['./fetch-data.component.css']
})
export class FetchDataComponent {
  public review?: CustomerReview;

  constructor(private http: HttpClient) {
    this.generate();
  }

  /**
   * Call the generate endpoint to get a newly constructed review.
   * @returns new CustomerReview object
   */
  public generate() {
    this.review = undefined;
    return this.http.get<string>('api/generate')
      .subscribe((result: string) => {
        let parsedJson = JSON.parse(JSON.stringify(result));
        this.review = {
          rating: parsedJson.Rating,
          summary: parsedJson.Summary
        };
      }, (error: any) => {
        this.review = undefined;
        console.error(error)
      });
  }
}

interface CustomerReview {
  summary: string;
  rating: number;
}
