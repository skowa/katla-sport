import { Component, OnInit } from '@angular/core';
import { HiveSection } from '../models/hive-section';
import { ActivatedRoute, Router } from '@angular/router';
import { HiveSectionService } from '../services/hive-section.service';
import { HiveService } from '../services/hive.service';
import { HiveListItem } from '../models/hive-list-item';

@Component({
  selector: 'app-hive-section-form',
  templateUrl: './hive-section-form.component.html',
  styleUrls: ['./hive-section-form.component.css']
})
export class HiveSectionFormComponent implements OnInit {
  hiveSection = new HiveSection(0, "", "", 1, false, "");
  existed = false;
  hiveId: number;
  hives: HiveListItem[];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private hiveSectionService: HiveSectionService,
    private hiveService: HiveService
  ) { }

  ngOnInit() {
    this.hiveService.getHives().subscribe(h => this.hives = h);
    this.route.params.subscribe(p => {
      this.hiveId = p['hiveId'];

      if (p['id'] === undefined) {
        this.hiveSection.storeHiveId = this.hiveId;
        return;
      }
      
      this.hiveSectionService.getHiveSection(p['id']).subscribe(h => this.hiveSection = h);
      this.existed = true;
    });
  }

  navigateToHiveSections() {
    this.router.navigate([`/hive/${this.hiveId}/sections`]);
  }

  onCancel() {
    this.navigateToHiveSections();
  }
  
  onSubmit() {
    if (this.existed) {
      this.hiveSectionService.updateHiveSection(this.hiveSection).subscribe(c => this.navigateToHiveSections());
    }
    else {
      this.hiveSectionService.addHiveSection(this.hiveSection).subscribe(c => this.navigateToHiveSections());
    }
  }

  onDelete() {
    this.hiveSectionService.setHiveSectionStatus(this.hiveSection.id, true).subscribe(c => this.hiveSection.isDeleted = true);
  }

  onUndelete() {
    this.hiveSectionService.setHiveSectionStatus(this.hiveSection.id, false).subscribe(c => this.hiveSection.isDeleted = false);
  }

  onPurge() {
    this.hiveSectionService.deleteHiveSection(this.hiveSection.id).subscribe(() => this.navigateToHiveSections());
  }
}
