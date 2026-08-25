import { Component } from '@angular/core';
import { FetchDataComponent } from '../fetch-data/fetch-data.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [FetchDataComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent {
}
